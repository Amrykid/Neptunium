using Crystal3.Core;
using Crystal3.IOC;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3.UI.MessageDialog;
using Neptunium.Data;
using Neptunium.Logging;
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
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel .ctor");

            if (!IoCManager.IsRegistered<IMessageDialogService>())
                IoCManager.Register<IMessageDialogService>(new DefaultMessageDialogService());

            GoToStationsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<StationsViewViewModel>())
                    InlineNavigationService.NavigateTo<StationsViewViewModel>();
            });

            GoToNowPlayingViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<NowPlayingViewViewModel>())
                    InlineNavigationService.NavigateTo<NowPlayingViewViewModel>();
            });

            GoToSettingsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<SettingsViewViewModel>())
                    InlineNavigationService.NavigateTo<SettingsViewViewModel>();
            });

            PlayCommand = new RelayCommand(x =>
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

            PauseCommand = new RelayCommand(x =>
            {
                if (BackgroundMediaPlayer.Current.CanPause)
                    BackgroundMediaPlayer.Current.Pause();
            }, x => { try { return BackgroundMediaPlayer.Current.CanPause; } catch (Exception) { return true; } });

            //WindowManager.GetStatusManagerForCurrentWindow().NormalStatusText = "Neptunium/Hanasu Alpha";
        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel OnNavigatedTo");

            InlineNavigationService = Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two);

            if (!ShoutcastStationMediaPlayer.IsInitialized)
                await ShoutcastStationMediaPlayer.InitializeAsync();

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            ShoutcastStationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            //var currentStationName = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.AppMediaId;

            //CurrentStation = currentStationName;

            //CurrentSong = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title;
            //CurrentArtist = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Artist;

            SendLaunchOrResumeMessageToAudioPlayer();
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
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel ShoutcastStationMediaPlayer_CurrentStationChanged");

            await App.Dispatcher.RunAsync(() =>
            {
                LogManager.Info(typeof(AppShellViewModel),
                    "AppShellViewModel ShoutcastStationMediaPlayer_CurrentStationChanged || Dispatcher_Delegate { CurrentStation: " +
                    (ShoutcastStationMediaPlayer.CurrentStation == null ? "null" : ShoutcastStationMediaPlayer.CurrentStation.Name) + " }");

                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation.Name;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });
        }

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel ShoutcastStationMediaPlayer_MetadataChanged");

            await App.Dispatcher.RunAsync(() =>
            {
                LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel ShoutcastStationMediaPlayer_MetadataChanged || Dispatcher_Delegate { CurrentStation: " +
                    (ShoutcastStationMediaPlayer.CurrentStation == null ? "null" : ShoutcastStationMediaPlayer.CurrentStation.Name) + " || CurrentSong: " + e.Title + " || CurrentArtist: " + e.Artist + " }"); 

                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation.Name;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });
        }

        protected override async Task OnResumingAsync()
        {
            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;

            if (!ShoutcastStationMediaPlayer.IsInitialized)
                await ShoutcastStationMediaPlayer.InitializeAsync();

            SendLaunchOrResumeMessageToAudioPlayer();

            await base.OnResumingAsync(); //only so that I can fake await the above method call.
        }

        private static void SendLaunchOrResumeMessageToAudioPlayer()
        {
            var payload = new ValueSet();
            payload.Add(Messages.AppLaunchOrResume, "");
            BackgroundMediaPlayer.SendMessageToBackground(payload);
        }

        protected override Task OnSuspendingAsync(IDictionary<string, object> data)
        {
            var payload = new ValueSet();
            payload.Add(Messages.AppSuspend, "");
            BackgroundMediaPlayer.SendMessageToBackground(payload);


            ShoutcastStationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;

            ShoutcastStationMediaPlayer.Deinitialize();

            return base.OnSuspendingAsync(data);
        }

        public string CurrentSong { get { return GetPropertyValue<string>("CurrentSong"); } private set { SetPropertyValue<string>("CurrentSong", value); } }
        public string CurrentArtist { get { return GetPropertyValue<string>("CurrentArtist"); } private set { SetPropertyValue<string>("CurrentArtist", value); } }
        public string CurrentStation { get { return GetPropertyValue<string>("CurrentStation"); } private set { SetPropertyValue<string>("CurrentStation", value); } }
        public string CurrentStationLogo { get { return GetPropertyValue<string>("CurrentStationLogo"); } private set { SetPropertyValue<string>("CurrentStationLogo", value); } }

        public RelayCommand GoToStationsViewCommand { get; private set; }
        public RelayCommand GoToNowPlayingViewCommand { get; private set; }
        public RelayCommand GoToSettingsViewCommand { get; private set; }

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }
    }
}
