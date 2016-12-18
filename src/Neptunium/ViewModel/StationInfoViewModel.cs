using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Data;
using Neptunium.Fragments;
using Crystal3.UI.Commands;
using Windows.System;

namespace Neptunium.ViewModel
{
    public class StationInfoViewModel : UIViewModelBase
    {
        public StationInfoViewModel()
        {
            SongHistory = new StationInfoViewSongHistoryFragment();

            GoToStationWebsiteCommand = new RelayCommand(async obj =>
            {
                if (obj == null || !(obj is StationModel)) return;

                StationModel station = (StationModel)obj;
                if (!string.IsNullOrWhiteSpace(station.Site))
                {
                    await Launcher.LaunchUriAsync(new Uri(station.Site));
                }
                else
                {
                    //todo snack bar that there isn't a site listed.
                }
                    
            });
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

        public RelayCommand GoToStationWebsiteCommand { get; private set; }
    }
}
