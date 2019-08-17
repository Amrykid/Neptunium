using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Metadata
{
    /// <summary>
    /// The base class for defining metadata sources.
    /// </summary>
    public abstract class BaseSongMetadataSource
    {
        /// <summary>
        /// Tries to find an album corresponding to a particular track, artist and locale.
        /// </summary>
        /// <param name="track">The track or song that is contained on the album.</param>
        /// <param name="artist">The artist who released the album.</param>
        /// <param name="locale">The locale of the artist when they released the album.</param>
        /// <returns>AlbumData or null</returns>
        public abstract Task<AlbumData> TryFindAlbumAsync(string track, string artist, string locale = "JP");
        /// <summary>
        /// Tries to find an artist by their name and locale.
        /// </summary>
        /// <param name="artistName">The artist's name.</param>
        /// <param name="locale">The locale of the artist which is used to narrow the search.</param>
        /// <returns>ArtistData or null</returns>
        public abstract Task<ArtistData> TryFindArtistAsync(string artistName, string locale = "JP");
        /// <summary>
        /// Gets an artist using a previously retrieved ID.
        /// </summary>
        /// <param name="artistID">The ID of the artist to retrieve.</param>
        /// <param name="locale">The locale of the artist which the ID corresponds to.</param>
        /// <returns>ArtistData or null</returns>
        public abstract Task<ArtistData> GetArtistAsync(string artistID, string locale = "JP");
        /// <summary>
        /// Tries to find information on a particular song.
        /// </summary>
        /// <param name="song">The song to search for.</param>
        /// <param name="locale">The locale for narrowing the search.</param>
        /// <returns></returns>
        public abstract Task TryFindSongAsync(ExtendedSongMetadata song, string locale = "JP");

        /// <summary>
        /// Checks to see if a URL is accessible over HTTP.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if it is accessible. False if it isn't.</returns>
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
