using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using System.Collections.ObjectModel;
using Neptunium.Core.Stations;
using Crystal3.UI.Commands;
using Neptunium.ViewModel.Dialog;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.UI;
using Windows.Foundation;

namespace Neptunium.ViewModel
{
    public class StationsPageViewModel : UIViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Network.IsConnectedChanged += Network_IsConnectedChanged;

            IsBusy = true;

            DetectNetworkStatus();

            if (AvailableStations == null || AvailableStations?.Count == 0)
            {
                try
                {
                    AvailableStations = new ObservableCollection<StationItem>((await NepApp.Stations.GetStationsAsync()).OrderBy(x => x.Name));
                    //AvailableStations = new StationsPageObservableVirtualizingCollection((await NepApp.Stations.GetStationsAsync()).Take(5));
                }
                catch (Exception ex)
                {
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh!", "An unexpected error occurred. " + ex.ToString());
                }
                finally {
                    IsBusy = false;
                }

                //SortedAvailableStations = new AdvancedCollectionView(AvailableStations, false);
                //SortedAvailableStations.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending, null));

                //NepApp.Stations.ObserveStationsAsync().Subscribe<StationItem>((StationItem item) =>
                //{
                //    AvailableStations.Add(item);
                //}, async (Exception ex) =>
                //{
                //    IsBusy = false;
                //    await NepApp.UI.ShowInfoDialogAsync("Uh-oh!", "An unexpected error occurred. " + ex.ToString());
                //}, () =>
                //{
                //    //when done
                //    LoadLastPlayedStation();
                //    IsBusy = false;
                //});

                //GroupedStations = AvailableStations.GroupBy(x => x.Group ?? "Ungrouped Stations").OrderBy(x => x.Key).Select(x => x);

            }
            else
            {
                IsBusy = false;
            }

            base.OnNavigatedTo(sender, e);
        }

        private void Network_IsConnectedChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                DetectNetworkStatus();
            });
        }

        private void DetectNetworkStatus()
        {
            NetworkAvailable = NepApp.Network.IsConnected;

            if (!NetworkAvailable)
            {
                NepApp.UI.Overlay.ShowSnackBarMessageAsync("Network disconnected.");
            }
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Network.IsConnectedChanged -= Network_IsConnectedChanged;

            //SortedAvailableStations.Clear();
            AvailableStations.Clear();

            SelectedStation = null;

            base.OnNavigatedFrom(sender, e);
        }

        public bool NetworkAvailable
        {
            get { return GetPropertyValue<bool>(); }
            private set { SetPropertyValue<bool>(value: value); }
        }

        public ObservableCollection<StationItem> AvailableStations
        {
            get { return GetPropertyValue<ObservableCollection<StationItem>>(); }
            private set { SetPropertyValue<ObservableCollection<StationItem>>(value: value); }
        }

        public IAdvancedCollectionView SortedAvailableStations
        {
            get { return GetPropertyValue<IAdvancedCollectionView>(); }
            private set { SetPropertyValue<IAdvancedCollectionView>(value: value); }
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
                if (NepApp.Network.NetworkUtilizationBehavior == NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.OptIn)
                {
                    if (await NepApp.UI.ShowYesNoDialogAsync("Data usage", "You're either over your data limit or roaming. Do you want to stream this station?") == false)
                    {
                        //user answered no
                        return;
                    }
                }

                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync(string.Format("Connecting to {0}...", stationItem.Name), "Please wait...");
                controller.SetIndeterminate();

                try
                {
                    StationStream stream = result.Selection as StationStream;
                    if (stream == null)
                    {
                        //check if we need to automatically choose a lower bitrate.
                        if ((int)NepApp.Network.NetworkUtilizationBehavior < 2) //check if we're on "conservative" or "opt-in"
                        {
                            //grab the stream with the lowest bitrate
                            stream = stationItem.Streams.OrderBy(x => x.Bitrate).First();
                        }
                        else
                        {
                            stream = stationItem.Streams.OrderByDescending(x => x.Bitrate).First(); //otherwise, grab a higher bitrate
                        }
                    }

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

        //public class StationsPageObservableVirtualizingCollection: ObservableCollection<StationItem>, ISupportIncrementalLoading
        //{
        //    public StationsPageObservableVirtualizingCollection(IEnumerable<StationItem> stationItems): base(stationItems)
        //    {

        //    }
        //    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        //    {
        //        return Task.Run(async () =>
        //        {
        //            var items = await NepApp.Stations.GetStationsAsync();
        //            var newItems = items.Skip(this.Count).Take((int)count);
        //            await App.Dispatcher.RunAsync(() =>
        //            {
        //                foreach (var item in newItems) Add(item);
        //            });
        //            return new LoadMoreItemsResult() { Count = (uint)newItems.Count() };
        //        }).AsAsyncOperation<LoadMoreItemsResult>();
        //    }

        //    public bool HasMoreItems => this.Count < NepApp.Stations.StationsCount;
        //}
    }
}
