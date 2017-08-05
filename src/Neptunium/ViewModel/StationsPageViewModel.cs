using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using System.Collections.ObjectModel;
using Neptunium.Core.Stations;
using Crystal3.UI.Commands;
using Neptunium.ViewModel.Dialog;

namespace Neptunium.ViewModel
{
    public class StationsPageViewModel: ViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (e.Direction == CrystalNavigationDirection.Forward)
            {
                AvailableStations = new ObservableCollection<StationItem>(await NepApp.Stations.GetStationsAsync());
            }

            base.OnNavigatedTo(sender, e);
        }

        public ObservableCollection<StationItem> AvailableStations
        {
            get { return GetPropertyValue<ObservableCollection<StationItem>>(); }
            private set { SetPropertyValue<ObservableCollection<StationItem>>(value: value); }
        }

        public StationItem SelectedStation
        {
            get { return GetPropertyValue<StationItem>(); }
            private set { SetPropertyValue<StationItem>(value: value); }
        }

        public RelayCommand ShowStationInfoCommand => new RelayCommand(async station =>
        {
            await NepApp.UI.Overlay.ShowDialogFragmentAsync<StationInfoDialogFragment>(station);
        });
    }
}
