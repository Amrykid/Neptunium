using System;

namespace Neptunium.Managers.Songs
{
    public class SongManagerSongChangedEventArgs: EventArgs
    {
        internal SongManagerSongChangedEventArgs()
        {

        }

        public bool IsUnknown { get; internal set; }
        internal SongMetadata Metadata { get; set; }
    }
}