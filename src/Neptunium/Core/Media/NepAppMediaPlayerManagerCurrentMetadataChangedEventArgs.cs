using Neptunium.Core.Media.Metadata;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs
    {
        public SongMetadata Metadata { get; private set; }

        internal NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(SongMetadata metadata)
        {
            Metadata = metadata;
        }
    }
}