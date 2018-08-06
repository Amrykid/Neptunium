using System;
using System.ComponentModel;
using System.Net;
using Windows.Networking.Connectivity;
using static Neptunium.NepApp;
using System.Linq;

namespace Neptunium
{
    public class NepAppNetworkManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        public NepAppNetworkManager()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            IsConnected = IsInternetConnected();
            DetectConnectionType();
        }

        private bool IsInternetConnected()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = (connections != null) &&
                (connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
            return internet;
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            bool oldStatus = IsConnected;
            bool newStatus = IsInternetConnected();
            IsConnected = newStatus;

            if (oldStatus != newStatus)
            {
                DetectConnectionType();
                IsConnectedChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged(nameof(IsConnected));
            }
        }

        private void DetectConnectionType()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();

            if (connections != null)
            {
                if (connections.IsWlanConnectionProfile)
                {
                    ConnectionType = NetworkConnectionType.WiFi;
                }
                else if (connections.IsWwanConnectionProfile)
                {
                    ConnectionType = NetworkConnectionType.CellularData;
                }
                else
                {
                    ConnectionType = NetworkConnectionType.Ethernet;
                    //dial-up?
                }
            }
            else
            {
                ConnectionType = NetworkConnectionType.Unknown; //none?
            }

            RaisePropertyChanged(nameof(ConnectionType));

            UpdateNetworkUtilizationBehavior();
        }

        internal IPAddress GetLocalIPAddress()
        {
            //based on: https://stackoverflow.com/a/33774534

            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;
            var hostname =
                NetworkInformation.GetHostNames()
                    .FirstOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            if (hostname != null)
            {
                // the ip address
                return IPAddress.Parse(hostname?.CanonicalName);
            }

            return null;
        }

        private void UpdateNetworkUtilizationBehavior()
        {
            //https://docs.microsoft.com/en-us/previous-versions/windows/apps/jj835821(v=win.10)
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();

            if (connections != null)
            {
                var cost = connections.GetConnectionCost();
                var dataPlan = connections.GetDataPlanStatus();

                if ((bool)NepApp.Settings.GetSetting(AppSettings.AutomaticallyConserveDataWhenOnMeteredConnections))
                {
                    if (cost.NetworkCostType == NetworkCostType.Unrestricted || cost.NetworkCostType == NetworkCostType.Unknown)
                    {
                        NetworkUtilizationBehavior = NetworkDeterminedAppBehaviorStyle.Normal;
                    }
                    else if (cost.NetworkCostType == NetworkCostType.Fixed || cost.NetworkCostType == NetworkCostType.Variable)
                    {
                        if (!cost.Roaming && !cost.OverDataLimit)
                        {
                            NetworkUtilizationBehavior = NetworkDeterminedAppBehaviorStyle.Conservative;
                        }
                        else
                        {
                            NetworkUtilizationBehavior = NetworkDeterminedAppBehaviorStyle.OptIn;
                        }
                    }
                }
                else
                {
                    NetworkUtilizationBehavior = NetworkDeterminedAppBehaviorStyle.Normal;
                }

                RaisePropertyChanged(nameof(NetworkUtilizationBehavior));
            }
        }

        public event EventHandler IsConnectedChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected { get; private set; }

        public NetworkDeterminedAppBehaviorStyle NetworkUtilizationBehavior { get; private set; }
        public NetworkConnectionType ConnectionType { get; private set; }

        public enum NetworkDeterminedAppBehaviorStyle
        {
            Normal = 2,
            Conservative = 1,
            OptIn = 0
        }

        public enum NetworkConnectionType
        {
            Ethernet = 3,
            WiFi = 2,
            CellularData = 1,
            Unknown = 0
        }

        private void RaisePropertyChanged(string propertyName)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}