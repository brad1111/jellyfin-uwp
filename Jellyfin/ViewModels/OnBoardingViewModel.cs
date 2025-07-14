using Jellyfin.Core;
using Jellyfin.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.ViewModels
{
    public class OnBoardingViewModel: IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OnBoardingStatus _onBoardingStatus = OnBoardingStatus.None;
        public OnBoardingStatus OnBoardingStatus
        {
            get => _onBoardingStatus;
            private set
            {
                _onBoardingStatus = value;
                if(value == OnBoardingStatus.Connecting)
                {
                    ErrorMessage = null; //reset error message on reconnect
                }
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(OnBoardingStatus)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(AcceptConnections)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ConnectError)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Connecting)));
            }
        } 
        public bool AcceptConnections => _onBoardingStatus != OnBoardingStatus.Connecting && _onBoardingStatus != OnBoardingStatus.Connected;
        public bool ConnectError => _onBoardingStatus == OnBoardingStatus.Error;
        public bool Connecting => _onBoardingStatus == OnBoardingStatus.Connecting;
        public ObservableCollection<DiscoveredServer> DiscoveredServers { get; } = new ObservableCollection<DiscoveredServer>();

        private string _errorMessage = null;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (value == string.Empty)
                {
                    _errorMessage = null;
                } 
                else
                {
                    _errorMessage = value;
                }
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }

        private string _uriString = string.Empty;
        public string UriString
        {
            get => _uriString;
            set
            {
                _uriString = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(UriString)));
            }
        }

        private Socket _socket = null;

        private async Task<bool> CheckURLValidAsync(string uriString)
        {
            // also do a check for valid url
            if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                return false;
            }

            //add scheme to uri if not included 
            Uri testUri = new UriBuilder(uriString).Uri;

            // check URL exists
            HttpWebRequest request;
            HttpWebResponse response;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(testUri);
                response = (HttpWebResponse)(await request.GetResponseAsync());
            }
            catch (WebException ex)
            {
                // Handle web exceptions here
                if (ex.Response != null && ex.Response is HttpWebResponse errorResponse)
                {
                    int statusCode = (int)errorResponse.StatusCode;
                    if (statusCode >= 300 && statusCode <= 308)
                    {
                        // Handle Redirect
                        string newLocation = errorResponse.Headers["Location"];
                        if (!string.IsNullOrEmpty(newLocation))
                        {
                            uriString = newLocation;
                            return await CheckURLValidAsync(uriString); // Recursively check the new location
                        }
                    }
                    else
                    {
                        ErrorMessage = statusCode.ToString();
                    }
                    return false;
                }
                else
                {
                    // Handle other exceptions
                    return false;
                }
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            var encoding = System.Text.Encoding.GetEncoding(response.CharacterSet);
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                string responseText = reader.ReadToEnd();
                if (!responseText.Contains("Jellyfin"))
                {
                    return false;
                }
            }

            // If everything is OK, update the URI before saving it
            Central.Settings.JellyfinServer = uriString;

            return true;
        }

        public async Task Connect()
        {
            OnBoardingStatus = OnBoardingStatus.Connecting;
            try
            {
                var ub = new UriBuilder(UriString);
                UriString = ub.ToString();
            }
            catch(FormatException)
            {
                //If the UriBuilder fails the following functions will handle the error
            }

            if (!await CheckURLValidAsync(UriString))
            {
                OnBoardingStatus = OnBoardingStatus.Error;
            }
            else
            {
                OnBoardingStatus = OnBoardingStatus.Connected;
                Central.Settings.JellyfinServer = UriString;
                (Window.Current.Content as Frame).Navigate(typeof(MainPage));
            }

        }

        public async Task Connect(DiscoveredServer discoveredServer)
        {
            UriString = discoveredServer.Address.ToString();
            await Connect();
        }

        public void DiscoverServers()
        {
            HandleDiscoverMessage(IPAddress.Broadcast);
        }

        private void HandleDiscoverMessage(IPAddress broadcastAddress)
        {
            if (_socket != null)
            {
                return;
            }

            var thread = new Thread(async () =>
            {
                var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ReceiveTimeout = 5000,
                };

                _socket = udpSocket;
                
                var sendbuf = Encoding.ASCII.GetBytes("Who is JellyfinServer?");
                var broadcastEndpoint = new IPEndPoint(broadcastAddress, 7359);
                try
                {
                    while (true)
                    {
               
                        udpSocket.SendTo(sendbuf, broadcastEndpoint);
                        Debug.WriteLine($"Sent to {broadcastAddress}");

                        var recieveBuffer = new byte[256];
                        try
                        {
                            while (true)
                            {
                                udpSocket.Receive(recieveBuffer);
                                var receivedText = Encoding.ASCII.GetString(recieveBuffer, 0, recieveBuffer.Length);
                                Debug.WriteLine($"Received: {receivedText}");
                                var discoveredServer = JsonConvert.DeserializeObject<DiscoveredServer>(receivedText);

                                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    if (!DiscoveredServers.Contains(discoveredServer))
                                    {
                                        DiscoveredServers.Add(discoveredServer);
                                    }
                                });
                            }
                        }
                        catch (SocketException ex)
                        {
                            if (ex.SocketErrorCode != SocketError.TimedOut)
                            {
                                throw ex;
                            }
                        }

                    }
                }
                catch (SocketException ex) 
                {
                    Debug.WriteLine($"Broadcast Address: {broadcastAddress}, errored with message: {ex.Message}");
                }
                finally
                {
                    udpSocket.Close();
                    _socket = null;
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        public void Dispose()
        {
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
        }
    }
}
