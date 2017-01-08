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
using Windows.UI.StartScreen;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Neptunium.Data.Stations;
using Neptunium.Services.Vibration;

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

                HapticFeedbackService.TapVibration();

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

            PinToStartCommand = new RelayCommand(async obj =>
            {
                if (obj == null || !(obj is StationModel)) return;

                HapticFeedbackService.TapVibration();

                StationModel station = (StationModel)obj;

                string tileId = station.Name.Replace(" ", "%20");

                if (!SecondaryTile.Exists(tileId))
                {
                    SecondaryTile tile = new SecondaryTile(tileId);
                    tile.VisualElements.BackgroundColor = await StationSupplementaryDataManager.GetStationLogoDominantColorAsync(station);
                    tile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Logo.scale-100.png", UriKind.Absolute);
                    tile.RoamingEnabled = true;
                    tile.DisplayName = station.Name;
                    tile.Arguments = "play-station?station=" + station.Name.Replace(" ", "%20");

                    bool result = await tile.RequestCreateAsync();

                    

                    //todo say results
                }
                else
                {
                    //todo say this already exists
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
        public RelayCommand PinToStartCommand { get; private set; }
    }
}
