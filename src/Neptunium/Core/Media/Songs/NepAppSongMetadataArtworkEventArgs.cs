using Neptunium.Core.Media.Metadata;
using System;

namespace Neptunium.Media.Songs
{
    public class NepAppSongMetadataArtworkEventArgs: EventArgs
    {
        internal NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground type, Uri url, SongMetadata songMetadata)
        {
            ArtworkType = type;
            ArtworkUri = url;
            CurrentMetadata = songMetadata;
        }
        public NepAppSongMetadataBackground ArtworkType { get; private set; }
        public Uri ArtworkUri { get; private set; }
        public SongMetadata CurrentMetadata { get; private set; }
    }
}