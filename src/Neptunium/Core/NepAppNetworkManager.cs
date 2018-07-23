using System;
using Windows.Networking.Connectivity;
using static Neptunium.NepApp;

namespace Neptunium
{
    public class NepAppNetworkManager : INepAppFunctionManager
    {
        public NepAppNetworkManager()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            IsConnected = IsInternetConnected();
            UpdateNetworkUtilizationBehavior();
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
                IsConnectedChanged?.Invoke(this, EventArgs.Empty);
                UpdateNetworkUtilizationBehavior();
            }
        }

        private void UpdateNetworkUtilizationBehavior()
        {
            //https://docs.microsoft.com/en-us/previous-versions/windows/apps/jj835821(v=win.10)
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();

            if (connections != null)
            {
                var cost = connections.GetConnectionCost();
                var dataPlan = connections.GetDataPlanStatus();

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
        }

        public event EventHandler IsConnectedChanged;
        public bool IsConnected { get; private set; }

        public NetworkDeterminedAppBehaviorStyle NetworkUtilizationBehavior { get; private set; }

        public enum NetworkDeterminedAppBehaviorStyle
        {
            Normal,
            Conservative,
            OptIn
        }
    }
}