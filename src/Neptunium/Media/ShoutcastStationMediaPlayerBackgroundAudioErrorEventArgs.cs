using Neptunium.Data;
using System;
using Windows.Media.Core;
using Windows.Networking.Connectivity;

namespace Neptunium.Media
{
    public class ShoutcastStationMediaPlayerBackgroundAudioErrorEventArgs: EventArgs
    {
        public StationModel Station { get; set; }
        public Exception Exception { get; set; }
        public MediaStreamSourceClosedReason ClosedReason { get; internal set; }
        public ConnectionProfile NetworkConnectionProfile { get; internal set; }
    }
}