using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using Crystal3.UI.Commands;

namespace Neptunium.ViewModel
{
    public class NowPlayingPageViewModel : ViewModelBase
    {
        public SongMetadata CurrentSong
        {
            get { return GetPropertyValue<SongMetadata>(); }
            set { SetPropertyValue<SongMetadata>(value: value); }
        }

        public StationItem CurrentStation
        {
            get { return GetPropertyValue<StationItem>(); }
            set { SetPropertyValue<StationItem>(value: value); }
        }

        public RelayCommand MediaCastingCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.ShowCastingPicker();
        });

        public RelayCommand ResumePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.Resume();
        });

        public RelayCommand PausePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.Pause();
        });

        public Uri Background
        {
            get { return GetPropertyValue<Uri>(); }
            private set { SetPropertyValue<Uri>(value: value); }
        }

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.SongArtworkProcessingComplete += SongManager_SongArtworkProcessingComplete;

            UpdateMetadata();
            UpdateBackground();

            base.OnNavigatedTo(sender, e);
        }

        private void SongManager_SongArtworkProcessingComplete(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateBackground();
            });
        }

        private void UpdateBackground()
        {
            Neptunium.Media.Songs.NepAppSongMetadataBackground backgroundType;
            if (NepApp.SongManager.IsSongArtworkAvailable(out backgroundType))
            {
                Background = NepApp.SongManager.GetSongArtworkUri(backgroundType);
            }
            else
            {
                if (NepApp.MediaPlayer.IsPlaying && NepApp.MediaPlayer.CurrentStream != null)
                {
                    if (!string.IsNullOrWhiteSpace(NepApp.MediaPlayer.CurrentStream.ParentStation.Background))
                    {
                        Background = new Uri(NepApp.MediaPlayer.CurrentStream.ParentStation.Background);
                    }
                }
            }
        }

        private void SongManager_SongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {

        }

        private void SongManager_PreSongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateMetadata();
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.SongManager.PreSongChanged -= SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged -= SongManager_SongChanged;
            NepApp.SongManager.SongArtworkProcessingComplete -= SongManager_SongArtworkProcessingComplete;

            base.OnNavigatedFrom(sender, e);
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.SongManager.CurrentSong;
            CurrentStation = NepApp.MediaPlayer.CurrentStream?.ParentStation;

            UpdateBackground();
        }
    }
}
