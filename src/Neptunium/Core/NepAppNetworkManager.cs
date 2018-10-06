using System;
using System.ComponentModel;
using System.Net;
using Windows.Networking.Connectivity;
using static Neptunium.NepApp;
using System.Linq;
using Windows.Networking;
using System.Collections.Generic;

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

        internal IEnumerable<IPAddress> GetLocalIPAddresses()
        {
            //based on: https://stackoverflow.com/a/33774534

            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) yield break;
            var hostnames =
                NetworkInformation.GetHostNames()
                    .Where(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            if (hostnames != null)
            {
                foreach (HostName host in hostnames)
                {
                    // the ip address
                    yield return IPAddress.Parse(host.CanonicalName);
                }
            }

            yield break;
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

        #region FROM: https://stackoverflow.com/a/43068327
        public IPAddress GetSubnetMask(IPAddress hostAddress)
        {
            var addressBytes = hostAddress.GetAddressBytes();

            if (addressBytes[0] >= 1 && addressBytes[0] <= 126)
                return IPAddress.Parse("255.0.0.0");
            else if (addressBytes[0] >= 128 && addressBytes[0] <= 191)
                return IPAddress.Parse("255.255.255.0");
            else if (addressBytes[0] >= 192 && addressBytes[0] <= 223)
                return IPAddress.Parse("255.255.255.0");
            else
                throw new ArgumentOutOfRangeException();
        }

        public IPAddress GetBroadastAddress(IPAddress hostIPAddress)
        {
            var subnetAddress = GetSubnetMask(hostIPAddress);

            var deviceAddressBytes = hostIPAddress.GetAddressBytes();
            var subnetAddressBytes = subnetAddress.GetAddressBytes();

            if (deviceAddressBytes.Length != subnetAddressBytes.Length)
                throw new ArgumentOutOfRangeException();

            var broadcastAddressBytes = new byte[deviceAddressBytes.Length];

            for (var i = 0; i < broadcastAddressBytes.Length; i++)
                broadcastAddressBytes[i] = (byte)(deviceAddressBytes[i] | subnetAddressBytes[i] ^ 255);

            return new IPAddress(broadcastAddressBytes);
        }
        #endregion
    }
}