using System;
using System.Threading.Tasks;
using Jellyfin.Core;
using Jellyfin.Utils;
using Jellyfin.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Controls;

/// <summary>
/// Represents a custom web view control for interacting with a Jellyfin server.
/// </summary>
public sealed partial class JellyfinWebView : UserControl
{
    private WebView2 _wView;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinWebView"/> class.
    /// </summary>
    public JellyfinWebView()
    {
        InitializeComponent();

        if (Central.Settings.JellyfinServerValidated)
        {
            InitialiseWebView();
        }
        else
        {
            BeginServerValidation();
        }
    }

    private void BeginServerValidation()
    {
        _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        {
            try
            {
                var jellyfinServerCheck = await ServerCheckUtil.IsJellyfinServerUrlValidAsync(new Uri(Central.Settings.JellyfinServer)).ConfigureAwait(true);
                // Check if the parsed URI is pointing to a Jellyfin server.
                if (!jellyfinServerCheck.IsValid)
                {
                    MessageDialog md = new MessageDialog($"The jellyfin server '{Central.Settings.JellyfinServer}' is currently not available: \r\n" +
                        $" {jellyfinServerCheck.ErrorMessage}");
                    await md.ShowAsync();
                    (Window.Current.Content as Frame).Navigate(typeof(OnBoarding));
                    return;
                }

                InitialiseWebView();
            }
            finally
            {
                ProgressIndicator.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void InitialiseWebView()
    {
        _wView = new WebView2();
        // Set WebView source
        _wView.Source = new Uri(Central.Settings.JellyfinServer);

        _wView.CoreWebView2Initialized += WView_CoreWebView2Initialized;
        _wView.NavigationCompleted += JellyfinWebView_NavigationCompleted;
    }

    private void WView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        // Must wait for CoreWebView2 to be initialized or the WebView2 would be unfocusable.
        Content = _wView;
        _wView.Focus(FocusState.Programmatic);

        // Set useragent to Xbox and WebView2 since WebView2 only sets these in Sec-CA-UA, which isn't available over HTTP.
        _wView.CoreWebView2.Settings.UserAgent += " WebView2 " + Utils.AppUtils.GetDeviceFormFactorType().ToString();

        _wView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false; // Disable autofill on Xbox as it puts down the virtual keyboard.
        _wView.CoreWebView2.ContainsFullScreenElementChanged += JellyfinWebView_ContainsFullScreenElementChanged;
    }

    private async void JellyfinWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            CoreWebView2WebErrorStatus errorStatus = args.WebErrorStatus;
            MessageDialog md = new MessageDialog($"Navigation failed: {errorStatus}");
            await md.ShowAsync();
        }

        ProgressIndicator.Visibility = Visibility.Collapsed;
    }

    private void JellyfinWebView_ContainsFullScreenElementChanged(CoreWebView2 sender, object args)
    {
        var appView = ApplicationView.GetForCurrentView();

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
}
