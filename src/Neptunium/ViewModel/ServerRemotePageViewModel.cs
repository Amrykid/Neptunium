using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Microsoft.Toolkit.Uwp.UI;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel
{
    public class ServerRemotePageViewModel : ViewModelBase
    {
        private Neptunium.Core.NepAppServerFrontEndManager.NepAppServerClient serverClient = null;
        private Neptunium.Core.NepAppServerFrontEndManager.NepAppServerDiscoverer serverFinder = null;

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            serverClient = new Core.NepAppServerFrontEndManager.NepAppServerClient();
            serverClient.SongChanged += ServerClient_SongChanged;
            serverFinder = new Core.NepAppServerFrontEndManager.NepAppServerDiscoverer();

            ConnectCommand = new RelayCommand(async parameter =>
            {
                if (!IsConnected)
                {
                    try
                    {
                        await serverClient.TryConnectAsync(IPAddress.Parse((string)parameter));
                        IsConnected = true;

                        //todo subscribe to an event from serverClient for receiving metadata.

                        LoadStations();
                    }
                    catch (Exception ex)
                    {
                        IsConnected = false;
                    }
                }
            });

            DisconnectCommand = new RelayCommand(parameter =>
            {
                if (IsConnected)
                {
                    serverClient?.Dispose();

                    AvailableStations.Clear();
                    SortedAvailableStations.Clear();
                }
            });

            SendStationCommand = new RelayCommand(parameter =>
            {
                if (IsConnected && parameter != null)
                {
                    serverClient?.AskServerToStreamStation((StationItem)parameter);
                }
            });

            SendStopCommand = new RelayCommand(parameter =>
            {
                if (IsConnected)
                {
                    serverClient?.AskServerToStop();
                }
            });

            RaisePropertyChanged(nameof(ConnectCommand));
            RaisePropertyChanged(nameof(DisconnectCommand));
            RaisePropertyChanged(nameof(SendStationCommand));
            RaisePropertyChanged(nameof(SendStopCommand));

            base.OnNavigatedTo(sender, e);
        }

        private void ServerClient_SongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                CurrentSong = e.Metadata;
            });
        }

        private void LoadStations()
        {
            AvailableStations = new ObservableCollection<StationItem>();
            SortedAvailableStations = new AdvancedCollectionView(AvailableStations, false);
            SortedAvailableStations.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending, null));

            NepApp.Stations.ObserveStationsAsync().Subscribe<StationItem>((StationItem item) =>
            {
                AvailableStations.Add(item);
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            serverClient.SongChanged -= ServerClient_SongChanged;          
            serverClient?.Dispose();
            serverFinder?.Dispose();

            base.OnNavigatedFrom(sender, e);
        }

        public bool IsConnected
        {
            get { return GetPropertyValue<bool>(); }
            private set
            {
                SetPropertyValue<bool>(value: value);
            }
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

        public RelayCommand ConnectCommand { get; private set; }

        public RelayCommand DisconnectCommand { get; private set; }

        public RelayCommand SendStationCommand { get; private set; }

        public RelayCommand SendStopCommand { get; private set; }

        public SongMetadata CurrentSong
        {
            get { return GetPropertyValue<SongMetadata>(); }
            private set { SetPropertyValue<SongMetadata>(value: value); }
        }
    }
}
