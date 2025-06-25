﻿using Jellyfin.Core;
using Jellyfin.Utils;
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
            WView.NavigationCompleted += JellyfinWebView_NavigationCompleted;
            SystemNavigationManager.GetForCurrentView().BackRequested += Back_BackRequested;

            // Initialize GamepadManager
            _gamepadManager = new GamepadManager();
            _gamepadManager.OnBackPressed += HandleGamepadBackPress;
        }

        private void HandleGamepadBackPress()
        {
            if (WView.CanGoBack)
            {
                WView.GoBack();
            }
        }

        private void WView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
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
                MessageDialog md = new MessageDialog($"Navigation failed: {errorStatus}");
                await md.ShowAsync();
                return;
            }

            await WView.ExecuteScriptAsync("navigator.gamepadInputEmulation = 'mouse';");
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
            _gamepadManager?.Dispose();
        }
    }
}