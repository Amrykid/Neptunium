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

            UpdateMetadata();
            UpdateBackground();

            base.OnNavigatedTo(sender, e);
        }

        private void SongManager_SongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            UpdateBackground();
        }

        private void SongManager_PreSongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateMetadata();
            });
        }

        private void UpdateBackground()
        {
            try
            {

                var extendedData = NepApp.SongManager.CurrentSongWithAdditionalMetadata;
                if (extendedData != null)
                {
                    if (extendedData?.Album != null)
                    {
                        if (!string.IsNullOrWhiteSpace(extendedData.Album?.AlbumCoverUrl))
                        {
                            App.Dispatcher.RunWhenIdleAsync(() =>
                            {
                                Background = new Uri(extendedData.Album?.AlbumCoverUrl);
                            });
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(extendedData.ArtistInfo?.ArtistImage))
                    {
                        App.Dispatcher.RunWhenIdleAsync(() =>
                        {
                            Background = new Uri(extendedData.ArtistInfo?.ArtistImage);
                        });
                    }
                    else if (extendedData.JPopAsiaArtistInfo != null)
                    {
                        //from JPopAsia
                        if (extendedData.JPopAsiaArtistInfo.ArtistImageUrl != null)
                        {
                            App.Dispatcher.RunWhenIdleAsync(() =>
                            {
                                Background = extendedData.JPopAsiaArtistInfo.ArtistImageUrl;
                            });
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.SongManager.PreSongChanged -= SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged -= SongManager_SongChanged;

            base.OnNavigatedFrom(sender, e);
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.SongManager.CurrentSong;
            CurrentStation = NepApp.MediaPlayer.CurrentStream?.ParentStation;

            if (!string.IsNullOrWhiteSpace(CurrentStation?.Background))
            {
                Background = new Uri(CurrentStation?.Background);
            }
        }
    }
}
