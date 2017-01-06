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
            StationMediaPlayer.IsPlayingChanged -= StationMediaPlayer_IsPlayingChanged;

            base.OnNavigatedFrom(e);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StationMediaPlayer.IsPlayingChanged += StationMediaPlayer_IsPlayingChanged;
            SetPlaybackButtonState(StationMediaPlayer.IsPlaying);

            base.OnNavigatedTo(e);
        }

        private void StationMediaPlayer_IsPlayingChanged(object sender, EventArgs e)
        {
            SetPlaybackButtonState(StationMediaPlayer.IsPlaying);

            if (StationMediaPlayer.IsPlaying)
                ColorPanel.StartAnimating();
            else
                ColorPanel.StopAnimating();
        }

        private void SetPlaybackButtonState(bool isPlaying)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                switch (isPlaying)
                {
                    case true:
                        PlayPauseButton.Content = new SymbolIcon(Symbol.Pause);
                        break;
                    case false:
                        PlayPauseButton.Content = new SymbolIcon(Symbol.Play);
                        break;
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus(FocusState.Programmatic);
            PlayPauseButton.Focus(FocusState.Programmatic);

            if (StationMediaPlayer.IsPlaying)
                ColorPanel.StartAnimating();

            //if (StationMediaPlayer.IsPlaying && StationMediaPlayer.CurrentStation != null)
            //{
            //    //var accentColor = (Color)this.Resources["SystemAccentColor"];

            //    GlassPanel.ChangeBlurColor(await Neptunium.Data.Stations.StationSupplementaryDataManager.GetStationLogoDominantColorAsync(StationMediaPlayer.CurrentStation));
            //}
        }
    }
}
