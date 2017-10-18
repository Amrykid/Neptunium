using Neptunium.Core.Media.Metadata;
using System;

namespace Neptunium.Media.Songs
{
    public class NepAppSongChangedEventArgs : EventArgs
    {
        public SongMetadata Metadata { get; private set; }

        internal NepAppSongChangedEventArgs(SongMetadata metadata)
        {
            Metadata = metadata;
        }
    }
}