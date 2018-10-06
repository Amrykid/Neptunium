using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Metadata
{
    public abstract class BaseSongMetadataSource
    {
        public abstract Task<AlbumData> TryFindAlbumAsync(string track, string artist, string locale = "JP");

        public abstract Task<ArtistData> TryFindArtistAsync(string artistName, string locale = "JP");

        public abstract Task<ArtistData> GetArtistAsync(string artistID, string locale = "JP");

        public abstract Task TryFindSongAsync(ExtendedSongMetadata song, string locale = "JP");

        protected async Task<bool> CheckIfUrlIsWebAccessibleAsync(Uri url)
        {
            HttpClient http = new HttpClient();

            try
            {
                bool result = false;
                var response = await http.GetAsync(url);

                result = response.IsSuccessStatusCode;

                return result;
            }

            catch (Exception) //todo narrow down the exact exception thrown
            {
                return false;
            }
            finally
            {
                http.Dispose();
            }
        }

    }
}
