using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;

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
                case "CurrentMetadata":
                    UpdateMetadata();
                    break;
            }
        }

        private void UpdateMetadata()
        {
            CurrentSong = NepApp.Media.CurrentMetadata;
            CurrentStation = NepApp.Media.CurrentStream?.ParentStation;
        }
    }
}
