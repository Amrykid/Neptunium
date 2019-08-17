using Neptunium.Core.Media.Metadata;
using System;

namespace Neptunium.Media.Songs
{
    public class NepAppStationMessageReceivedEventArgs: EventArgs
    {
        public NepAppStationMessageReceivedEventArgs(SongMetadata songMetadata)
        {
            StationMessage = songMetadata.ToString();
        }

        public string StationMessage { get; private set; }
    }
}