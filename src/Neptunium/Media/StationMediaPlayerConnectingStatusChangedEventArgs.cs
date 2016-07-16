using System;

namespace Neptunium.Media
{
    public class StationMediaPlayerConnectingStatusChangedEventArgs: EventArgs
    {
        internal StationMediaPlayerConnectingStatusChangedEventArgs(bool isConnecting)
        {
            IsConnecting = isConnecting;
        }

        public bool IsConnecting { get; private set; }
    }
}