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
using Crystal3.Messaging;
using Windows.UI;

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

        public Uri CoverImage
        {
            get { return GetPropertyValue<Uri>(); }
            private set { SetPropertyValue<Uri>(value: value); }
        }

        public bool IsPlaying
        {
            get { return GetPropertyValue<bool>(); }
            private set { SetPropertyValue<bool>(value: value); }
        }

        public bool IsMediaEngaged
        {
            get { return GetPropertyValue<bool>(); }
            private set { SetPropertyValue<bool>(value: value); }
        }

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.MediaPlayer.IsPlayingChanged += MediaPlayer_IsPlayingChanged;
            NepApp.MediaPlayer.MediaEngagementChanged += MediaPlayer_MediaEngagementChanged;
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.ArtworkProcessor.SongArtworkProcessingComplete += SongManager_SongArtworkProcessingComplete;

            IsPlaying = NepApp.MediaPlayer.IsPlaying;
            IsMediaEngaged = NepApp.MediaPlayer.IsMediaEngaged;

            UpdateMetadataFollowedByArtwork();

            base.OnNavigatedTo(sender, e);
        }

        private void MediaPlayer_MediaEngagementChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsPlaying = NepApp.MediaPlayer.IsPlaying;
                IsMediaEngaged = NepApp.MediaPlayer.IsMediaEngaged;
            });
        }

        private void MediaPlayer_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsPlaying = NepApp.MediaPlayer.IsPlaying;
                UpdateMetadataFollowedByArtwork();
            });
        }

        private void SongManager_SongArtworkProcessingComplete(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateArtwork();
            });
        }

        private void UpdateArtwork()
        {
            if (NepApp.MediaPlayer.CurrentStream != null && CurrentStation != null && IsMediaEngaged)
            {
                var albumArt = NepApp.SongManager.ArtworkProcessor.GetSongArtworkUri(Media.Songs.NepAppSongMetadataBackground.Album);
                var artistArt = NepApp.SongManager.ArtworkProcessor.GetSongArtworkUri(Media.Songs.NepAppSongMetadataBackground.Artist);

                Background = artistArt ?? (!string.IsNullOrWhiteSpace(CurrentStation.Background) ? new Uri(CurrentStation.Background) : null);

                if (Background == null)
                {
                    CoverImage = albumArt ?? CurrentStation.StationLogoUrl;
                }
                else
                {
                    CoverImage = null;
                }
            }
            else
            {
                CoverImage = null;
                Background = null;
            }
        }

        private void SongManager_SongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
        }

        private void SongManager_PreSongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateMetadataFollowedByArtwork();
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.MediaPlayer.MediaEngagementChanged -= MediaPlayer_MediaEngagementChanged;
            NepApp.MediaPlayer.IsPlayingChanged -= MediaPlayer_IsPlayingChanged;
            NepApp.SongManager.PreSongChanged -= SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged -= SongManager_SongChanged;
            NepApp.SongManager.ArtworkProcessor.SongArtworkProcessingComplete -= SongManager_SongArtworkProcessingComplete;

            base.OnNavigatedFrom(sender, e);
        }

        private async void UpdateMetadataFollowedByArtwork()
        {
            CurrentSong = NepApp.SongManager.GetCurrentSongOrUnknown();
            CurrentStation = await NepApp.Stations.GetStationByNameAsync(NepApp.MediaPlayer.CurrentStream?.ParentStation);

            UpdateArtwork();
        }
    }
}
