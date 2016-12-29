using Crystal3.Core;
using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Managers.Songs;
using Neptunium.Media;
using Neptunium.Services.SnackBar;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Neptunium.Fragments
{
    public class NowPlayingViewFragment : ViewModelFragment
    {
        public SleepTimerFlyoutViewFragment SleepTimerViewFragment { get; private set; }
        public HandOffFlyoutViewFragment HandOffViewFragment { get; private set; }

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }

        public RelayCommand GoToNowPlayingPageCommand { get; private set; }

        public StationModel CurrentStation { get { return GetPropertyValue<StationModel>(); } private set { SetPropertyValue<StationModel>(value: value); } }

        public string CurrentSong { get { return GetPropertyValue<string>("CurrentSong"); } private set { SetPropertyValue<string>("CurrentSong", value); } }
        public string CurrentArtist { get { return GetPropertyValue<string>("CurrentArtist"); } private set { SetPropertyValue<string>("CurrentArtist", value); } }
        public string CurrentStationLogo { get { return GetPropertyValue<string>("CurrentStationLogo"); } private set { SetPropertyValue<string>("CurrentStationLogo", value); } }

        public string SongMetadata
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value: value); }
        }

        public NowPlayingViewFragment()
        {
            SleepTimerViewFragment = new SleepTimerFlyoutViewFragment();
            HandOffViewFragment = new HandOffFlyoutViewFragment();

            PlayCommand = new RelayCommand(x =>
            {
                if (!StationMediaPlayer.IsPlaying)
                    StationMediaPlayer.Play();
            });

            PauseCommand = new RelayCommand(x =>
            {
                if (StationMediaPlayer.IsPlaying)
                    StationMediaPlayer.Pause();
            });


            GoToNowPlayingPageCommand = new RelayCommand(x =>
            {
                NavigationService inlineNavService = WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(FrameLevel.Two);

                if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                    inlineNavService.NavigateTo<NowPlayingViewViewModel>();
            });

            CurrentStation = StationMediaPlayer.CurrentStation;

            SongManager.PreSongChanged += SongManager_PreSongChanged;
            SongManager.SongChanged += SongManager_SongChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (SongManager.CurrentSong != null)
                SongMetadata = SongManager.CurrentSong.Track + " by " + SongManager.CurrentSong.Artist;
        }

        private async void SongManager_PreSongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            await App.Dispatcher.RunAsync(IUIDispatcherPriority.High, () =>
             {
                 SongMetadata = e.Metadata.Track + " by " + e.Metadata.Artist;


                 CurrentSong = e.Metadata.Track;
                 CurrentArtist = e.Metadata.Artist;

                 try
                 {
                     if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                     {
                         if (!WindowManager.GetNavigationManagerForCurrentWindow().GetNavigationServiceFromFrameLevel(FrameLevel.Two)
                         .IsNavigatedTo<NowPlayingViewViewModel>())
                             IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync("Now Playing: " + SongMetadata, 6000);
                     }
                 }
                 catch (Exception) { }
             });
        }

        private async void SongManager_SongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                if (CurrentSong != e.Metadata.Track)
                {
                    SongMetadata = e.Metadata.Track + " by " + e.Metadata.Artist;


                    CurrentSong = e.Metadata.Track;
                    CurrentArtist = e.Metadata.Artist;
                }

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    CurrentStationLogo = StationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });
        }

        private async void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsBusy = true;

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    CurrentStationLogo = StationMediaPlayer.CurrentStation?.Logo.ToString();
                }
                else
                {
                    CurrentArtist = "";
                    CurrentSong = "";
                    CurrentStation = null;
                    CurrentStationLogo = null;
                }

                IsBusy = false;
            });
        }

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, StationMediaPlayerBackgroundAudioErrorEventArgs e)
        {
            if (!e.StillPlaying)
            {
                await Crystal3.CrystalApplication.Dispatcher.RunAsync(() =>
                {
                    CurrentArtist = "";
                    CurrentSong = "";
                    CurrentStation = null;
                    CurrentStationLogo = null;
                });
            }
        }

        public sealed override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void Invoke(ViewModelBase viewModel, object data)
        {

        }
    }
}
