using System;

namespace Neptunium.Managers
{
    public class SongMetadataManagerFoundAlbumMetadataEventArgs: EventArgs
    {
        internal SongMetadataManagerFoundAlbumMetadataEventArgs()
        {

        }

        public Data.AlbumData FoundAlbumData { get; internal set; }
        public string QueiredArtist { get; internal set; }
        public string QueriedTrack { get; internal set; }
    }
}