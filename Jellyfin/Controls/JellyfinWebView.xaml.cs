using Jellyfin.Core;
using Jellyfin.Utils;
using Jellyfin.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
    using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Controls
{
    public sealed partial class JellyfinWebView : UserControl, IDisposable
    {
        private readonly GamepadManager _gamepadManager;

        public JellyfinWebView()
        {
            this.InitializeComponent();

            // Set WebView source
            WView.Source = new Uri(Central.Settings.JellyfinServer);
            
           

            WView.CoreWebView2Initialized += WView_CoreWebView2Initialized;
            WView.NavigationStarting += WView_NavigationStarting;
            WView.NavigationCompleted += JellyfinWebView_NavigationCompleted;
            SystemNavigationManager.GetForCurrentView().BackRequested += Back_BackRequested;

            // Initialize GamepadManager
            _gamepadManager = new GamepadManager();
            _gamepadManager.OnBackPressed += HandleGamepadBackPress;
        }

        private async Task WView_NavigationStartingTask(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            // Workaround to fix focus issues with gamepad
            BtnFocusStealer.Focus(FocusState.Programmatic);

            // Force layout=tv and enabledGamepad on xbox only.
            if (AppUtils.IsXbox)
            {
                await WView.ExecuteScriptAsync("localStorage.setItem(\"layout\", \"tv\")");
                await WView.ExecuteScriptAsync("localStorage.setItem(\"enableGamepad\", \"true\")");
            }
        }
        private async void WView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            await WView_NavigationStartingTask(sender, args);
        }

        private void HandleGamepadBackPress()
        {
            // redundant as jellyfin handles back presses
        }

        private void WView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            // Set useragent to Xbox
            WView.CoreWebView2.Settings.UserAgent += " UWP " + Utils.AppUtils.GetDeviceFormFactorType().ToString();
            WView.CoreWebView2.ContainsFullScreenElementChanged += JellyfinWebView_ContainsFullScreenElementChanged;
        }

        private void Back_BackRequested(object sender, BackRequestedEventArgs args)
        {
            if (WView.CanGoBack)
            {
                WView.GoBack();
            }
            args.Handled = true;
        }

        private async Task JellyfinWebView_NavigationCompletedTask(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                CoreWebView2WebErrorStatus errorStatus = args.WebErrorStatus;
                ErrorDialog ed = new ErrorDialog(errorStatus.ToString());
                ed.PrimaryButtonClick += (s, e) =>
                {
                    WView.Reload();
                };
                ed.SecondaryButtonClick += (s, e) =>
                {
                    (Window.Current.Content as Frame).Navigate(typeof(OnBoarding));
                };
                await ed.ShowAsync();
            } 
            else
            {
                // Hacky way of forcing webview to set focus on web content.
                WView.Focus(FocusState.Programmatic);
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        private async void JellyfinWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            await JellyfinWebView_NavigationCompletedTask(sender, args);
        }

        private void JellyfinWebView_ContainsFullScreenElementChanged(CoreWebView2 sender, object args)
        {
            ApplicationView appView = ApplicationView.GetForCurrentView();

            if (sender.ContainsFullScreenElement)
            {
                appView.TryEnterFullScreenMode();
                return;
            }

            if (appView.IsFullScreenMode)
            {
                appView.ExitFullScreenMode();
            }
        }

        public void Dispose()
        {
            WView.Close();
            _gamepadManager?.Dispose();
        }

    }
}