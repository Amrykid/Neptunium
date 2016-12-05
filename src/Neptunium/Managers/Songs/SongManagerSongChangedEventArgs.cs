using System;

namespace Neptunium.Managers.Songs
{
    public class SongManagerSongChangedEventArgs: EventArgs
    {
        internal SongManagerSongChangedEventArgs()
        {

        }

        internal SongMetadata Metadata { get; set; }
    }
}