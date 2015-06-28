using Crystal3.Core;
using Crystal3.IOC;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3.UI.MessageDialog;
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
            if (!IoCManager.IsRegistered<IMessageDialogService>())
                IoCManager.Register<IMessageDialogService>(new DefaultMessageDialogService());
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
            }, x =>
            {
                var currentPlayerState = BackgroundMediaPlayer.Current.CurrentState;

                return currentPlayerState != MediaPlayerState.Buffering &&
                currentPlayerState != MediaPlayerState.Opening &&
                currentPlayerState != MediaPlayerState.Playing &&
                currentPlayerState != MediaPlayerState.Closed;
            });

            PauseCommand = new CRelayCommand(x =>
            {
                if (BackgroundMediaPlayer.Current.CanPause)
                    BackgroundMediaPlayer.Current.Pause();
            }, x => { try { return BackgroundMediaPlayer.Current.CanPause; } catch (Exception) { return true; } });

            if (!ShoutcastStationMediaPlayer.IsInitialized)
                ShoutcastStationMediaPlayer.Initialize();

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            ShoutcastStationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

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

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, EventArgs e)
        {
            ShoutcastStationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError; //throttle error messages

            await IoCManager.Resolve<IMessageDialogService>().ShowAsync("We are unable to play this station.", "Error while trying to play this station.");

            await Crystal3.CrystalApplication.Dispatcher.RunAsync(() =>
            {
                CurrentArtist = "";
                CurrentSong = "";
                CurrentStation = null;
                CurrentStationLogo = null;
            });

            ShoutcastStationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;
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
            if (!ShoutcastStationMediaPlayer.IsInitialized)
                ShoutcastStationMediaPlayer.Initialize();

            //try
            //{
            //    ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            //    ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            //}
            //catch (Exception) { }

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


            //ShoutcastStationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
            //ShoutcastStationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;

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
