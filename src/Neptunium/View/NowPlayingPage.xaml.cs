using Crystal3.Messaging;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.NowPlayingPageViewModel),
        Crystal3.Navigation.NavigationViewSupportedPlatform.Desktop | Crystal3.Navigation.NavigationViewSupportedPlatform.Mobile)]
    [Neptunium.Core.UI.NepAppUINoChromePage()]
    public sealed partial class NowPlayingPage : Page
    {
        private FrameNavigationService inlineNavigationService = null;
        public NowPlayingPage()
        {
            this.InitializeComponent();

            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().GetNavigationServiceFromFrameLevel(FrameLevel.Two) as FrameNavigationService;

            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                //https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/
                compactViewButton.Visibility = Visibility.Visible;
            }

            //if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Desktop)
            //{
            //    fullScreenButton.Visibility = Visibility.Visible;
            //}
        }

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdatePlaybackStatus(e.IsPlaying);
            });
        }

        private void UpdatePlaybackStatus(bool isPlaying)
        {
            if (isPlaying)
            {
                playPauseButton.SetValue(ToolTipService.ToolTipProperty, "Pause");
                playPauseButton.Content = new SymbolIcon(Symbol.Pause);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).PausePlaybackCommand;
            }
            else
            {
                playPauseButton.SetValue(ToolTipService.ToolTipProperty, "Play");
                playPauseButton.Content = new SymbolIcon(Symbol.Play);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).ResumePlaybackCommand;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            NepApp.MediaPlayer.IsPlayingChanged -= Media_IsPlayingChanged;

            base.OnNavigatingFrom(e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePlaybackStatus(NepApp.MediaPlayer.IsPlaying);
        }

        private async void compactViewButton_Click(object sender, RoutedEventArgs e)
        {
            //switch to compact overlay mode
            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            compactOptions.CustomSize = new Windows.Foundation.Size(320, 280);

            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(compactOptions.CustomSize);

            if (modeSwitched)
            {
                inlineNavigationService.SafeNavigateTo<CompactNowPlayingPageViewModel>();
            }
        }

        private void HandoffButton_Click(object sender, RoutedEventArgs e)
        {
            Messenger.SendMessageAsync("ShowHandoffFlyout", "");
        }
    }
}
