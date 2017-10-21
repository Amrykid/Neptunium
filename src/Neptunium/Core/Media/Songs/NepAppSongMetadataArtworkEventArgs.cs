using System;

namespace Neptunium.Core.Media.Songs
{
    public class NepAppSongMetadataArtworkEventArgs: EventArgs
    {
        internal NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground type, Uri url)
        {
            ArtworkType = type;
            ArtworkUri = url;
        }
        public NepAppSongMetadataBackground ArtworkType { get; private set; }
        public Uri ArtworkUri { get; private set; }
    }
}