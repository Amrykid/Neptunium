using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Data;
using Neptunium.Media;
using Neptunium.Services.SnackBar;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Fragments
{
    public class NowPlayingViewFragment : ViewModelFragment
    {
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
            CurrentStation = StationMediaPlayer.CurrentStation;

            StationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (StationMediaPlayer.SongMetadata != null)
                SongMetadata = StationMediaPlayer.SongMetadata.Track + " by " + StationMediaPlayer.SongMetadata.Artist;
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

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, EventArgs e)
        {
            await Crystal3.CrystalApplication.Dispatcher.RunAsync(() =>
            {
                CurrentArtist = "";
                CurrentSong = "";
                CurrentStation = null;
                CurrentStationLogo = null;
            });

        }

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Contains(e.Title)) return; //ignore that pre-defined station message that happens every so often.

            if (!string.IsNullOrWhiteSpace(e.Title) && string.IsNullOrWhiteSpace(e.Artist))
            {
                //station message got through.

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#else
                return;
#endif
            }

            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                SongMetadata = e.Title + " by " + e.Artist;


                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    CurrentStationLogo = StationMediaPlayer.CurrentStation.Logo.ToString();
                }


                try
                {
                    if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                    {
                        if (!WindowManager.GetNavigationManagerForCurrentWindow().GetNavigationServiceFromFrameLevel(FrameLevel.Two).IsNavigatedTo<NowPlayingViewViewModel>())
                            IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync("Now Playing: " + SongMetadata, 6000);
                    }
                }
                catch (Exception) { }
            });
        }

        public override void Dispose()
        {

        }

        public override void Invoke(ViewModelBase viewModel, object data)
        {

        }
    }
}
