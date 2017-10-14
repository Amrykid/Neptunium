using Crystal3;
using Crystal3.Navigation;
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
    [Crystal3.Navigation.NavigationViewModel(typeof(CompactNowPlayingPageViewModel), Crystal3.Navigation.NavigationViewSupportedPlatform.Desktop | Crystal3.Navigation.NavigationViewSupportedPlatform.Mobile)]
    [Neptunium.Core.UI.NepAppUINoChromePage()]
    public sealed partial class CompactNowPlayingPage : Page
    {
        private FrameNavigationService inlineNavigationService = null;

        public CompactNowPlayingPage()
        {
            this.InitializeComponent();

            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().GetNavigationServiceFromFrameLevel(FrameLevel.Two) as FrameNavigationService;
        }

        private async void compactViewButton_Click(object sender, RoutedEventArgs e)
        {
            //switch back to regular mode
            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default, ViewModePreferences.CreateDefault(ApplicationViewMode.Default));
            if (modeSwitched)
            {
                inlineNavigationService.GoBack();
            }
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
                playPauseButton.Label = "Pause";
                playPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).PausePlaybackCommand;
            }
            else
            {
                playPauseButton.Label = "Play";
                playPauseButton.Icon = new SymbolIcon(Symbol.Play);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).ResumePlaybackCommand;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    //prevent navigation until we're switched out of compact overlay mode.
                    e.Cancel = true;
                    return;
                }
            }

            NepApp.Media.IsPlayingChanged -= Media_IsPlayingChanged;

            base.OnNavigatingFrom(e);
        }
    }
}
