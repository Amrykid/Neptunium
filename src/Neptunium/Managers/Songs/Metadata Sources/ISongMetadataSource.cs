using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers.Songs.Metadata_Sources
{
    public interface ISongMetadataSource
    {
        Task<AlbumData> TryFindAlbumAsync(string track, string artist);
        Task<ArtistData> TryFindArtistAsync(string artistName);
        Task<ArtistData> GetArtistAsync(string artistID);
    }
}
