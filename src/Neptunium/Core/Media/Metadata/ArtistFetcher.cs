using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;

namespace Neptunium.Core.Media.Metadata
{
    /// <summary>
    /// Formerlly: Small utility class to grab artist data from JPopAsia.com - This use to be a feature in the original version of Neptunium (when it was called Hanasu)
    /// </summary>
    public static class ArtistFetcher
    {
        public static async Task<JPopAsiaArtistData> FindArtistDataOnJPopAsiaAsync(string artistName)
        {
            HttpClient http = new HttpClient();
            HttpResponseMessage httpResponse = null;

            Uri directUri = new Uri(
                string.Format("http://www.jpopasia.com/{0}/",
                    Uri.EscapeUriString(
                        artistName.Replace(" ", "")
                        .Replace("!", "")
                        .Replace("-", "")
                        .Replace("*", "")))); //try and use a direct url. this works for artists like "Superfly" or "Perfume"

            httpResponse = await http.GetAsync(directUri);
            if (httpResponse.IsSuccessStatusCode)
            {
                return await ParseArtistPageForDataAsync(artistName, httpResponse);
            }
            else
            {
                //we're gonna have to search

                //pull up our pre-cached list of artists and search there.
                var builtInList = await GetBuiltinArtistEntriesAsync();
                BuiltinArtistEntry builtInMatch = builtInList.FirstOrDefault(x => x.Name.ToLower().Equals(artistName.ToLower()));

                if (builtInMatch == null)
                {
                    builtInMatch = builtInList.FirstOrDefault(x =>
                    {
                        if (x.Name.ToLower().FuzzyEquals(artistName.ToLower(), .9)) return true;

                        if (artistName.Contains(" ")) //e.g. "Ayumi Hamasaki" vs. "Hamasaki Ayumi"
                        {
                            //string lastNameFirstNameSwappedName = string.Join(" ", artistName.Split(' ').Reverse()); //splices, reverses and joins: "Ayumi Hamasaki" -> ["Ayumi","Hamasaki"] -> ["Hamasaki", "Ayumi"] -> "Hamasaki Ayumi"

                            return x.AltNames.Any(str => str.FuzzyEquals(artistName.ToLower(), .9));
                        }

                        return false;
                    });
                }

                if (builtInMatch != null)
                {
                    httpResponse = await http.GetAsync(directUri);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        return await ParseArtistPageForDataAsync(artistName, httpResponse);
                    }
                }

                //manually search if we reach this point.
                //todo manually search.
            }

            http.Dispose();

            return null;
        }

        public static async Task<IEnumerable<BuiltinArtistEntry>> GetBuiltinArtistEntriesAsync()
        {
            var file = MetadataFinder.BuiltInArtistsFile;
            var reader = await file.OpenReadAsync();
            XDocument xmlDoc = XDocument.Load(reader.AsStream());

            List<BuiltinArtistEntry> artists = new List<BuiltinArtistEntry>();

            foreach (var artistElement in xmlDoc.Element("Artists").Elements("Artist"))
            {
                var artistEntry = new BuiltinArtistEntry();

                artistEntry.Name = artistElement.Attribute("Name").Value;
                artistEntry.JPopAsiaUrl = new Uri(artistElement.Attribute("JPopAsiaUrl").Value);

                if (artistElement.Elements("AltName") != null)
                {
                    artistEntry.AltNames = artistElement.Elements("AltName").Select(x => x.Value).ToArray();
                }

                if (artistElement.Attribute("FanArtTVUrl") != null)
                    artistEntry.FanArtTVUrl = new Uri(artistElement.Attribute("FanArtTVUrl").Value);

                artists.Add(artistEntry);
            }

            xmlDoc = null;
            reader.Dispose();

            return artists;
        }

        private static async Task<JPopAsiaArtistData> ParseArtistPageForDataAsync(string artistName, HttpResponseMessage httpResponse)
        {
            JPopAsiaArtistData result = new JPopAsiaArtistData();
            result.ArtistName = artistName;

            try
            {
                string html = await httpResponse.Content.ReadAsStringAsync();

                var artistImageLine = Regex.Match(html, @"class=\""img-responsive rounded\"".+?>", RegexOptions.Singleline);
                if (artistImageLine.Success)
                {
                    var artistImageSrcValue = Regex.Match(artistImageLine.Value, "src=\".+?\"", RegexOptions.Singleline);

                    if (artistImageSrcValue.Success)
                    {
                        var value = artistImageSrcValue.Value.Substring(artistImageSrcValue.Value.IndexOf("\"")).Trim('\"');
                        if (value.StartsWith("//"))
                            value = "http:" + value;

                        result.ArtistImageUrl = new Uri(value);
                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
                httpResponse.Dispose();
            }

            //todo cache

            return result;
        }
    }

    public class BuiltinArtistEntry
    {
        public string Name { get; set; }
        public Uri JPopAsiaUrl { get; set; }
        public string[] AltNames { get; set; }
        public Uri FanArtTVUrl { get; set; }
    }

    public class JPopAsiaArtistData
    {
        public Uri ArtistImageUrl { get; internal set; }
        public string ArtistName { get; internal set; }
    }
}
