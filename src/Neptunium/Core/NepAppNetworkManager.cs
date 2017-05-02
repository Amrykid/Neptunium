using Windows.Networking.Connectivity;
using static Neptunium.NepApp;

namespace Neptunium
{
    public class NepAppNetworkManager: INepAppFunctionManager
    {
        public NepAppNetworkManager()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            IsConnected = IsInternetConnected();
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
                //todo fire an event stating that the connection changed.
            }
        }

        public bool IsConnected { get; private set; }
    }
}