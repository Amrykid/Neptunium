using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Neptunium.Core.Media.Metadata
{
    public static class FanArtTVFetcher
    {
        public static async Task<Uri> FetchArtistBackgroundAsync(string name)
        {
            var builtinArtists = await ArtistFetcher.GetBuiltinArtistEntriesAsync();
            var artist = builtinArtists.FirstOrDefault(x => x.Name == name || x.AltName == name);
            if (artist == null) return null;
            if (artist.FanArtTVUrl == null) return null;

            HttpClient http = new HttpClient();
            HttpResponseMessage httpResponse = null;

            httpResponse = await http.GetAsync(artist.FanArtTVUrl);
            if (!httpResponse.IsSuccessStatusCode) return null;


        }
    }
}
