using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Data;

namespace Neptunium.ViewModel
{
    public class StationInfoViewModel : UIViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (e.Direction == CrystalNavigationDirection.Forward || e.Direction == CrystalNavigationDirection.Refresh)
            {
                IsBusy = true;

                string stationName = e.Parameter as string;
                if (!string.IsNullOrWhiteSpace(stationName))
                {
                    if (!StationDataManager.IsInitialized)
                        await StationDataManager.InitializeAsync();

                    StationModel station = StationDataManager.Stations.FirstOrDefault(x => x.Name == stationName);

                    Station = station;
                }

                IsBusy = false;
            }

            base.OnNavigatedTo(sender, e);
        }

        public StationModel Station
        {
            get { return GetPropertyValue<StationModel>(); }
            private set { SetPropertyValue<StationModel>(value: value); }
        }
    }
}
