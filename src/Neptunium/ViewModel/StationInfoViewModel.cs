using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Data;
using Neptunium.Fragments;

namespace Neptunium.ViewModel
{
    public class StationInfoViewModel : UIViewModelBase
    {
        public StationInfoViewModel()
        {
            SongHistory = new StationInfoViewSongHistoryFragment();
        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (e.Direction == CrystalNavigationDirection.Forward || e.Direction == CrystalNavigationDirection.Refresh)
            {
                IsBusy = true;

                await UI.WaitForUILoadAsync();

                string stationName = e.Parameter as string;
                if (!string.IsNullOrWhiteSpace(stationName))
                {
                    StationModel station = StationDataManager.Stations.FirstOrDefault(x => x.Name == stationName);

                    await App.Dispatcher.RunWhenIdleAsync(() =>
                    {
                        Station = station;

                        SongHistory.Invoke(this, station);
                    });
                    
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

        public StationInfoViewSongHistoryFragment SongHistory { get; private set; }
    }
}
