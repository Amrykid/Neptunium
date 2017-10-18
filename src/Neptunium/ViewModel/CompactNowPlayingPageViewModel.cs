using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core.Stations;
using Neptunium.Core.Media.Metadata;
using Crystal3.UI.Commands;

namespace Neptunium.ViewModel
{
    public class CompactNowPlayingPageViewModel: ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.MediaPlayer.CurrentMetadataChanged += Media_CurrentMetadataChanged;

            UpdateMetadata();

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.MediaPlayer.CurrentMetadataChanged -= Media_CurrentMetadataChanged;

            base.OnNavigatedFrom(sender, e);
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.MediaPlayer.CurrentMetadata;
            CurrentStation = NepApp.MediaPlayer.CurrentStream?.ParentStation;
        }

        private void Media_CurrentMetadataChanged(object sender, Media.NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateMetadata();
            });
        }

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
            NepApp.MediaPlayer.Resume();
        });

        public RelayCommand PausePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.Pause();
        });
    }
}
