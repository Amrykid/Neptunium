using Crystal3.Model;
using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace Neptunium.ViewModel
{
    public class StationsViewViewModel : ViewModelBase
    {
        public StationsViewViewModel()
        {
            PlayStationCommand = new CRelayCommand(station =>
            {
                ShoutcastStationMediaPlayer.PlayStation((StationModel)station);
            }, station => station != null);
        }

        protected override async void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            base.OnNavigatedTo(sender, e);

            await StationDataManager.InitializeAsync();
            Stations = new ObservableCollection<StationModel>(StationDataManager.Stations);
        }

        protected override void OnNavigatedFrom(object sender, NavigationEventArgs e)
        {
            base.OnNavigatedFrom(sender, e);
        }

        public CRelayCommand PlayStationCommand { get; private set; }

        public ObservableCollection<StationModel> Stations
        {
            get { return GetPropertyValue<ObservableCollection<StationModel>>("Stations"); }
            private set { SetPropertyValue<ObservableCollection<StationModel>>("Stations", value); }
        }
    }
}
