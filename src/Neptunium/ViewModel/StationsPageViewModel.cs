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
using Windows.System;

namespace Neptunium.ViewModel
{
    public class StationsPageViewModel : UIViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (AvailableStations == null || AvailableStations?.Count == 0)
            {
                IsBusy = true;
                AvailableStations = new ObservableCollection<StationItem>((await NepApp.Stations.GetStationsAsync())?.OrderBy(x => x.Name));
                //GroupedStations = AvailableStations.GroupBy(x => x.Group ?? "Ungrouped Stations").OrderBy(x => x.Key).Select(x => x);

                LastPlayedStation = NepApp.Stations.LastPlayedStationName;
                if (!string.IsNullOrWhiteSpace(LastPlayedStation))
                {
                    var station = await NepApp.Stations.GetStationByNameAsync(LastPlayedStation);
                    if (station != null)
                    {
                        LastPlayedStationLogoUrl = station.StationLogoUrl;
                        LastPlayedStationDescription = station.Description;
                    }
                }

                IsBusy = false;
            }

            base.OnNavigatedTo(sender, e);
        }

        public ObservableCollection<StationItem> AvailableStations
        {
            get { return GetPropertyValue<ObservableCollection<StationItem>>(); }
            private set { SetPropertyValue<ObservableCollection<StationItem>>(value: value); }
        }

        public IEnumerable<IGrouping<string, StationItem>> GroupedStations
        {
            get { return GetPropertyValue<IEnumerable<IGrouping<string, StationItem>>>(); }
            private set { SetPropertyValue<IEnumerable<IGrouping<string, StationItem>>>(value: value); }
        }

        public StationItem SelectedStation
        {
            get { return GetPropertyValue<StationItem>(); }
            private set { SetPropertyValue<StationItem>(value: value); }
        }

        public string LastPlayedStation
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }

        public Uri LastPlayedStationLogoUrl
        {
            get { return GetPropertyValue<Uri>(); }
            private set { SetPropertyValue<Uri>(value: value); }
        }

        public string LastPlayedStationDescription
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }

        public RelayCommand OpenStationWebsiteCommand => new RelayCommand(async station =>
        {
            StationItem stationItem = (StationItem)station;
            await Launcher.LaunchUriAsync(new Uri(stationItem.Site));
        });

        public RelayCommand PinStationCommand => new RelayCommand(async station =>
        {
            StationItem stationItem = (StationItem)station;

            if (!NepApp.UI.Notifier.CheckIfStationTilePinned(stationItem))
            {
                try
                {
                    bool result = await NepApp.UI.Notifier.PinStationAsTileAsync(stationItem);
                }
                catch (Exception)
                {
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh!", "Wasn't able to pin the station.");
                }
            }
            else
            {
                await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "This station is already pinned!");
            }
        });

        public RelayCommand ShowStationInfoCommand => new RelayCommand(async station =>
        {
            StationItem stationItem = (StationItem)station;
            if (NepApp.MediaPlayer.CurrentStreamer?.StationPlaying == stationItem) return; //don't show a dialog to play the current station

            var result = await NepApp.UI.Overlay.ShowDialogFragmentAsync<StationInfoDialogFragment>(stationItem);
            if (result.ResultType == Core.UI.NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive)
            {
                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync(string.Format("Connecting to {0}...", stationItem.Name), "Please wait...");
                controller.SetIndeterminate();

                try
                {
                    StationStream stream = result.Selection as StationStream;
                    if (stream == null) stream = stationItem.Streams[0];
                    await NepApp.MediaPlayer.TryStreamStationAsync(stream);
                    await controller.CloseAsync();
                }
                catch (Neptunium.Core.NeptuniumException ex)
                {
                    await controller.CloseAsync();
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh! Couldn't do that!", ex.Message);
                }
            }
        });
    }
}
