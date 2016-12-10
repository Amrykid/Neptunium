using Crystal3.Navigation;
using Neptunium.Media;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View.Xbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(NowPlayingViewViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxNowPlayingView : Page
    {
        public XboxNowPlayingView()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StationMediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

            base.OnNavigatedFrom(e);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StationMediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            SetPlaybackButtonState(StationMediaPlayer.PlaybackSession);

            base.OnNavigatedTo(e);
        }

        private void PlaybackSession_PlaybackStateChanged(Windows.Media.Playback.MediaPlaybackSession sender, object args)
        {
            SetPlaybackButtonState(sender);
        }

        private void SetPlaybackButtonState(Windows.Media.Playback.MediaPlaybackSession sender)
        {
            if (sender == null) return;

            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                switch (sender.PlaybackState)
                {
                    case Windows.Media.Playback.MediaPlaybackState.Playing:
                    case Windows.Media.Playback.MediaPlaybackState.Opening:
                    case Windows.Media.Playback.MediaPlaybackState.Buffering:
                        PlayPauseButton.Content = new SymbolIcon(Symbol.Pause);
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Paused:
                        PlayPauseButton.Content = new SymbolIcon(Symbol.Play);
                        break;
                }
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus(FocusState.Programmatic);
            PlayPauseButton.Focus(FocusState.Programmatic);

            if (StationMediaPlayer.IsPlaying)
            {
                //var accentColor = (Color)this.Resources["SystemAccentColor"];

                GlassPanel.ChangeBlurColor(await Neptunium.Data.Stations.StationSupplementaryDataManager.GetStationLogoDominantColorAsync(StationMediaPlayer.CurrentStation));
            }
        }
    }
}
