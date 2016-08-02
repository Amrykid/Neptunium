using Crystal3;
using Crystal3.Core;
using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3.UI.MessageDialog;
using Neptunium.Data;
using Neptunium.Fragments;
using Neptunium.Logging;
using Neptunium.Media;
using Neptunium.Shared;
using NotificationsExtensions;
using NotificationsExtensions.Tiles;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Navigation;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel : UIViewModelBase
    {
        private NavigationService InlineNavigationService = null;
        public AppShellViewModel()
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel .ctor");

            if (!IoC.Current.IsRegistered<IMessageDialogService>())
                IoC.Current.Register<IMessageDialogService>(new DefaultMessageDialogService());

            GoToStationsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<StationsViewViewModel>())
                    InlineNavigationService.NavigateTo<StationsViewViewModel>();
            });

            GoToNowPlayingViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<NowPlayingViewFragment>())
                    InlineNavigationService.NavigateTo<NowPlayingViewFragment>();
            });

            GoToSettingsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<SettingsViewViewModel>())
                    InlineNavigationService.NavigateTo<SettingsViewViewModel>();
            });

            GoToSongHistoryViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<SongHistoryViewModel>())
                    InlineNavigationService.NavigateTo<SongHistoryViewModel>();
            });

            PlayCommand = new RelayCommand(x =>
            {
                BackgroundMediaPlayer.Current.Play();
            }, x =>
            {
                var currentPlayerState = BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState;

                return currentPlayerState != MediaPlaybackState.Buffering &&
                currentPlayerState != MediaPlaybackState.Opening &&
                currentPlayerState != MediaPlaybackState.Playing;
            });

            PauseCommand = new RelayCommand(x =>
            {
                if (BackgroundMediaPlayer.Current.PlaybackSession.CanPause)
                    BackgroundMediaPlayer.Current.Pause();
            }, x => { try { return BackgroundMediaPlayer.Current.PlaybackSession.CanPause; } catch (Exception) { return true; } });

            NowPlayingView = new NowPlayingViewFragment();

            WindowManager.GetStatusManagerForCurrentWindow().NormalStatusText = "Hanasu Alpha";

            NowPlayingView.UpdateLiveTile();
        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel OnNavigatedTo");

            InlineNavigationService = Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two);

            if (!StationMediaPlayer.IsInitialized)
                await StationMediaPlayer.InitializeAsync();

            StationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;


            StationMediaPlayer.ConnectingStatusChanged += StationMediaPlayer_ConnectingStatusChanged;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            StationMediaPlayer.ConnectingStatusChanged -= StationMediaPlayer_ConnectingStatusChanged;

            base.OnNavigatedFrom(sender, e);
        }


        private void StationMediaPlayer_ConnectingStatusChanged(object sender, StationMediaPlayerConnectingStatusChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() => IsBusy = e.IsConnecting);
        }

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, EventArgs e)
        {
            StationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError; //throttle error messages

            await IoC.Current.Resolve<IMessageDialogService>().ShowAsync("We are unable to play this station.", "Error while trying to play this station.");

            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;
        }

        private void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel ShoutcastStationMediaPlayer_CurrentStationChanged");
        }

        private void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            LogManager.Info(typeof(AppShellViewModel), "AppShellViewModel ShoutcastStationMediaPlayer_MetadataChanged");
        }

        

        public RelayCommand GoToStationsViewCommand { get; private set; }
        public RelayCommand GoToNowPlayingViewCommand { get; private set; }
        public RelayCommand GoToSettingsViewCommand { get; private set; }
        public RelayCommand GoToSongHistoryViewCommand { get; private set; }

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }

        public NowPlayingViewFragment NowPlayingView { get; private set; }
    }
}
