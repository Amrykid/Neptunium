using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Media;
using Neptunium.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Xaml.Navigation;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel : ViewModelBase
    {
        private NavigationService InlineNavigationService = null;
        public AppShellViewModel()
        {

        }

        protected override void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            InlineNavigationService = Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two);

            GoToStationsViewCommand = new CRelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<StationsViewViewModel>())
                    InlineNavigationService.NavigateTo<StationsViewViewModel>();
            });

            GoToNowPlayingViewCommand = new CRelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<NowPlayingViewViewModel>())
                    InlineNavigationService.NavigateTo<NowPlayingViewViewModel>();
            });

            PlayCommand = new CRelayCommand(x =>
            {
                BackgroundMediaPlayer.Current.Play();
            });

            PauseCommand = new CRelayCommand(x =>
            {
                if (BackgroundMediaPlayer.Current.CanPause)
                    BackgroundMediaPlayer.Current.Pause();
            });

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;

            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                try
                {
                    //var currentStationName = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.AppMediaId;

                    //CurrentStation = currentStationName;

                    //CurrentSong = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title;
                    //CurrentArtist = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Artist;

                    SendResumeMessageToAudioPlayer();
                }
                catch (Exception) { }
            }
        }

        private async void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            await App.Dispatcher.RunAsync(() =>
            {
                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation.Name;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });
        }

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            await App.Dispatcher.RunAsync(() =>
            {
                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation.Name;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });
        }

        protected override Task OnResumingAsync()
        {
            SendResumeMessageToAudioPlayer();

            return base.OnResumingAsync();
        }

        private static void SendResumeMessageToAudioPlayer()
        {
            var payload = new ValueSet();
            payload.Add(Messages.AppLaunchOrResume, "");
            BackgroundMediaPlayer.SendMessageToBackground(payload);
        }

        protected override Task OnSuspendingAsync(object data)
        {
            var payload = new ValueSet();
            payload.Add(Messages.AppSuspend, "");
            BackgroundMediaPlayer.SendMessageToBackground(payload);

            return base.OnSuspendingAsync(data);
        }

        public string CurrentSong { get { return GetPropertyValue<string>("CurrentSong"); } private set { SetPropertyValue<string>("CurrentSong", value); } }
        public string CurrentArtist { get { return GetPropertyValue<string>("CurrentArtist"); } private set { SetPropertyValue<string>("CurrentArtist", value); } }
        public string CurrentStation { get { return GetPropertyValue<string>("CurrentStation"); } private set { SetPropertyValue<string>("CurrentStation", value); } }
        public string CurrentStationLogo { get { return GetPropertyValue<string>("CurrentStationLogo"); } private set { SetPropertyValue<string>("CurrentStationLogo", value); } }

        public CRelayCommand GoToStationsViewCommand { get; private set; }
        public CRelayCommand GoToNowPlayingViewCommand { get; private set; }

        public CRelayCommand PlayCommand { get; private set; }
        public CRelayCommand PauseCommand { get; private set; }
    }
}
