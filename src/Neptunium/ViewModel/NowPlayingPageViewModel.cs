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
    public class NowPlayingPageViewModel: ViewModelBase
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
            NepApp.Media.PropertyChanged += Media_PropertyChanged;

            UpdateMetadata();

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Media.PropertyChanged -= Media_PropertyChanged;

            base.OnNavigatedFrom(sender, e);
        }

        private void Media_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "CurrentMetadata": //todo fix these hard coded strings
                    UpdateMetadata();
                    break;
                case "CurrentMetadataExtended":
                    {
                        if (NepApp.Media.CurrentMetadataExtended.Album != null)
                        {
                            var extendedData = NepApp.Media.CurrentMetadataExtended;

                            if (!string.IsNullOrWhiteSpace(extendedData.Album?.AlbumCoverUrl))
                            {
                                App.Dispatcher.RunWhenIdleAsync(() =>
                                {
                                    Background = new Uri(extendedData.Album?.AlbumCoverUrl);
                                });
                            }
                            //todo else if for artist background?
                        }
                    }
                    break;
            }
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.Media.CurrentMetadata;
            CurrentStation = NepApp.Media.CurrentStream?.ParentStation;

            if (Background == null && !string.IsNullOrWhiteSpace(CurrentStation?.Background))
            {
                App.Dispatcher.RunWhenIdleAsync(() =>
                {
                    Background = new Uri(CurrentStation?.Background);
                });
            }
        }
    }
}
