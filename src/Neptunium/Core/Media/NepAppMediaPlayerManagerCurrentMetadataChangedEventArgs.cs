using Neptunium.Core.Media.Metadata;
using System;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs: EventArgs
    {
        public SongMetadata Metadata { get; private set; }

        internal NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(SongMetadata metadata)
        {
            Metadata = metadata;
        }
    }
}