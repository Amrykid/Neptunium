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
            NepApp.Media.ShowCastingPicker();
        });

        public RelayCommand ResumePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.Media.Resume();
        });

        public RelayCommand PausePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.Media.Pause();
        });

        public Uri Background
        {
            get { return GetPropertyValue<Uri>(); }
            private set { SetPropertyValue<Uri>(value: value); }
        }

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Media.CurrentMetadataChanged += Media_CurrentMetadataChanged;
            NepApp.Media.CurrentMetadataExtendedInfoFound += Media_CurrentMetadataExtendedInfoFound;

            UpdateMetadata();
            UpdateBackground();

            base.OnNavigatedTo(sender, e);
        }

        private void Media_CurrentMetadataExtendedInfoFound(object sender, Media.NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs e)
        {
            UpdateBackground();
        }

        private void UpdateBackground()
        {
            try
            {

                var extendedData = NepApp.Media.CurrentMetadataExtended;
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

        private void Media_CurrentMetadataChanged(object sender, Media.NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateMetadata();
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Media.CurrentMetadataChanged -= Media_CurrentMetadataChanged;
            NepApp.Media.CurrentMetadataExtendedInfoFound -= Media_CurrentMetadataExtendedInfoFound;

            base.OnNavigatedFrom(sender, e);
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.Media.CurrentMetadata;
            CurrentStation = NepApp.Media.CurrentStream?.ParentStation;

            if (!string.IsNullOrWhiteSpace(CurrentStation?.Background))
            {
                Background = new Uri(CurrentStation?.Background);
            }
        }
    }
}
