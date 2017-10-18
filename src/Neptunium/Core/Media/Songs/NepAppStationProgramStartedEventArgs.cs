using System;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;

namespace Neptunium.Media.Songs
{
    public class NepAppStationProgramStartedEventArgs : EventArgs
    {
        public StationProgram RadioProgram { get; internal set; }
        public string Station { get; internal set; }
        public SongMetadata Metadata { get; internal set; }
    }
}