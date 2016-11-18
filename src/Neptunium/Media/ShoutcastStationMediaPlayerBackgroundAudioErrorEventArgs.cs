using Neptunium.Data;
using System;

namespace Neptunium.Media
{
    public class ShoutcastStationMediaPlayerBackgroundAudioErrorEventArgs: EventArgs
    {
        public StationModel Station { get; set; }
        public Exception Exception { get; set; }
    }
}