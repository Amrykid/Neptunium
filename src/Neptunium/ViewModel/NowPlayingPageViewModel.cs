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

        public Color BackgroundColor
        {
            get { return GetPropertyValue<Color>(); }
            private set { SetPropertyValue<Color>(value: value); }
        }

        public Uri CoverImage
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
            UpdateArtwork();

            base.OnNavigatedTo(sender, e);
        }

        private void SongManager_SongArtworkProcessingComplete(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateArtwork();
            });
        }

        private async void UpdateArtwork()
        {
            var albumArt = NepApp.SongManager.GetSongArtworkUri(Media.Songs.NepAppSongMetadataBackground.Album);
            if (albumArt != null)
            {
                CoverImage = albumArt;
            }
            else
            {
                CoverImage = NepApp.MediaPlayer.CurrentStream?.ParentStation?.StationLogoUrl;
            }

            Neptunium.Media.Songs.NepAppSongMetadataBackground backgroundType;
            if (NepApp.SongManager.IsSongArtworkAvailable(out backgroundType))
            {
                //update the background
                Background = NepApp.SongManager.GetSongArtworkUri(backgroundType);
            }
            else
            {
                if (NepApp.MediaPlayer.IsPlaying && NepApp.MediaPlayer.CurrentStream != null)
                {
                    if (!string.IsNullOrWhiteSpace(NepApp.MediaPlayer.CurrentStream.ParentStation.Background))
                    {
                        //update the background
                        Background = new Uri(NepApp.MediaPlayer.CurrentStream.ParentStation.Background);
                    }
                    else
                    {
                        Background = null;
                    }
                }
            }

            if (Background != null)
            {
                var backgroundColor = await StationSupplementaryDataManager.GetDominantColorAsync(Background);
                if (backgroundColor != BackgroundColor)
                {
                    BackgroundColor = backgroundColor;
                    await Messenger.SendMessageAsync("NowPlayingBgColor", backgroundColor);
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

            UpdateArtwork();
        }
    }
}
