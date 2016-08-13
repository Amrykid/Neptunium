using System;

namespace Neptunium.Managers
{
    public class SongMetadataManagerFoundMetadataEventArgs: EventArgs
    {
        internal SongMetadataManagerFoundMetadataEventArgs()
        {

        }

        public Data.AlbumData FoundAlbumData { get; internal set; }
        public string QueiredArtist { get; internal set; }
        public string QueriedTrack { get; internal set; }
    }
}