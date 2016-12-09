using Neptunium.Managers.Songs.Metadata_Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;

namespace Neptunium.Managers.Songs
{
    public class ITunesMetadataSource : ISongMetadataSource
    {
        iTunesSearch.Library.iTunesSearchManager itunesStore = null;
        public ITunesMetadataSource()
        {
            //itunesStore = new iTunesSearch.Library.iTunesSearchManager();
        }
        public Task<ArtistData> GetArtistAsync(string artistID)
        {
            throw new NotImplementedException();
        }

        public Task<AlbumData> TryFindAlbumAsync(string track, string artist)
        {
            throw new NotImplementedException();
        }

        public Task<ArtistData> TryFindArtistAsync(string artistName)
        {
            throw new NotImplementedException();
        }
    }
}
