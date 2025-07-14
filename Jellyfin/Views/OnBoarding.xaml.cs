using Jellyfin.Core;
using Jellyfin.Models;
using Jellyfin.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Jellyfin.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnBoarding : Page
    {
        private OnBoardingViewModel _viewModel;
        public OnBoarding()
        {
            this.InitializeComponent();
            this.Loaded += OnBoarding_Loaded;
            this._viewModel = (OnBoardingViewModel) this.DataContext;
        }

        private async void TxtUrl_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await _viewModel.Connect();
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.Connect();
        }

        private void OnBoarding_Loaded(object sender, RoutedEventArgs e)
        {
            txtUrl.Focus(FocusState.Programmatic);
            _viewModel.DiscoverServers();
        }

        private async void DiscoveredList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DiscoveredServer discoveredServer)
            {
                await _viewModel.Connect(discoveredServer);
            }

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Dispose();
        }
    }
}