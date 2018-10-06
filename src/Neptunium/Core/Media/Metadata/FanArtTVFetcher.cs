using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Neptunium.Core.Media.Metadata
{
    public static class FanArtTVFetcher
    {
        public static async Task<Uri> FetchArtistBackgroundAsync(string name)
        {
            var builtinArtists = await ArtistFetcher.GetBuiltinArtistEntriesAsync();
            var artist = builtinArtists.FirstOrDefault(x => x.Name.ToLower() == name.ToLower() || x.AltNames.Any(str => str.ToLower() == name.ToLower()));
            if (artist == null) return null;
            if (artist.FanArtTVUrl == null) return null;

            if (!NepApp.Network.IsConnected) return null;

            HttpClient http = new HttpClient();
            HttpResponseMessage httpResponse = null;

            httpResponse = await http.GetAsync(artist.FanArtTVUrl);
            if (!httpResponse.IsSuccessStatusCode) return null;

            try
            {
                string html = await httpResponse.Content.ReadAsStringAsync();

                var artistBackgroundBlock = Regex.Match(html, @"<ul class=\""artwork artistbackground\"">(.+?)</ul>", RegexOptions.Singleline);
                if (artistBackgroundBlock.Success)
                {
                    var artistBGBlockHtml = artistBackgroundBlock.Value;

                    var images = Regex.Matches(artistBGBlockHtml,
                        @"<a rel=\""artistbackground\"".+?href=\""(.+?)\"".+?>", RegexOptions.Singleline);

                    if (images.Count > 0)
                    {
                        var groups = images[0].Groups;

                        if (groups.Count > 0)
                        {
                            var imgSrc = groups[groups.Count - 1].Value;

                            return new Uri("https://fanart.tv" + imgSrc);
                        }
                    }
                }
            }
            finally
            {
                httpResponse.Dispose();
            }

            return null;
        }
    }
}
