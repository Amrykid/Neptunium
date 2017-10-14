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

        public RelayCommand OpenStationWebsiteCommand => new RelayCommand(async station =>
        {
            StationItem stationItem = (StationItem)station;
            await Launcher.LaunchUriAsync(new Uri(stationItem.Site));
        });

        public RelayCommand ShowStationInfoCommand => new RelayCommand(async station =>
        {
            StationItem stationItem = (StationItem)station;
            if (NepApp.Media.CurrentStreamer?.StationPlaying == stationItem) return; //don't show a dialog to play the current station

            if ((await NepApp.UI.Overlay.ShowDialogFragmentAsync<StationInfoDialogFragment>(stationItem)).ResultType == Core.UI.NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive)
            {
                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync(string.Format("Connecting to {0}...", stationItem.Name), "Please wait...");
                controller.SetIndeterminate();

                try
                {
                    await NepApp.Media.TryStreamStationAsync(stationItem.Streams[0]);
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
