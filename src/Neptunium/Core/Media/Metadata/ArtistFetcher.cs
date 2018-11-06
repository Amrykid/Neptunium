using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;

namespace Neptunium.Core.Media.Metadata
{
    /// <summary>
    /// Formerly: Small utility class to grab artist data from JPopAsia.com - This use to be a feature in the original version of Neptunium (when it was called Hanasu)
    /// </summary>
    public static class ArtistFetcher
    {
        /// <summary>
        /// Finds an artist on JPopAsia.com.
        /// </summary>
        /// <param name="artistName">The name of the artist to search for.</param>
        /// <param name="stationLocale">The locale of the artist to search for.</param>
        /// <returns></returns>
        public static async Task<JPopAsiaArtistData> FindArtistDataOnJPopAsiaAsync(string artistName, string stationLocale)
        {
            //Sets up an http client and response object.
            HttpClient http = new HttpClient();
            HttpResponseMessage httpResponse = null;

            BuiltinArtistEntry builtInMatch = NepApp.MetadataManager.FindBuiltInArtist(artistName, stationLocale);

            //If we found a match, access it here.
            if (builtInMatch != null)
            {
                if (builtInMatch.JPopAsiaUrl != null)
                {
                    //Sends the request to the match.
                    httpResponse = await http.GetAsync(builtInMatch.JPopAsiaUrl);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        //The request was successful as noted by the response. Try and scrape the page here.
                        return await ParseArtistPageForDataAsync(artistName, httpResponse);
                    }
                }
            }

            //Some artists are available directly by putting their name into the url. We create a URL for that here.
            Uri directUri = new Uri(
                string.Format("http://www.jpopasia.com/{0}/",
                    Uri.EscapeUriString(
                        artistName.Replace(" ", "")
                        .Replace("!", "")
                        .Replace("-", "")
                        .Replace("*", "")))); //try and use a direct url. this works for artists like "Superfly" or "Perfume"

            try
            {
                //Try to access the artist via a direct url.
                httpResponse = await http.GetAsync(directUri);
                if (httpResponse.IsSuccessStatusCode)
                {
                    //The artist was successfuly reached from the direct url, we can scrape the page here.
                    return await ParseArtistPageForDataAsync(artistName, httpResponse);
                }
                else
                {
                    //We couldn't get to the artist from a direct url here, We're gonna have to search

                    //Manually search if we reach this point.
                    //TODO manually search.
                }
            }
            catch (Exception)
            {
                //Upon an error, return null.
                return null;
            }
            finally
            {
                //Clean up http objects here.
                if (httpResponse != null)
                {
                    httpResponse.Dispose();
                    httpResponse = null;
                }
                http.Dispose();
            }

            //If all else fails, return null.
            return null;
        }

        /// <summary>
        /// Scrapes the artist page on JPopAsia.com
        /// </summary>
        /// <param name="artistName">The name of the artist represented on the page.</param>
        /// <param name="httpResponse">The http response containing the HTML of the page.</param>
        /// <returns>JPopAsiaArtistData or null</returns>
        private static async Task<JPopAsiaArtistData> ParseArtistPageForDataAsync(string artistName, HttpResponseMessage httpResponse)
        {
            //Creates a JPopAsiaArtistData representing the artist.
            JPopAsiaArtistData result = new JPopAsiaArtistData();
            result.ArtistName = artistName;

            try
            {
                //Grabs the HTML from the http response.
                string html = await httpResponse.Content.ReadAsStringAsync();

                //Ues regex to grab the div containing the artist's image on the page.
                var artistImageLine = Regex.Match(html, @"class=\""img-responsive rounded\"".+?>", RegexOptions.Singleline);
                if (artistImageLine.Success) //Check if the match was successful.
                {
                    //Tries to match the "src" attribute of the img element.
                    var artistImageSrcValue = Regex.Match(artistImageLine.Value, "src=\".+?\"", RegexOptions.Singleline);

                    if (artistImageSrcValue.Success) //Checks if the match was successful.
                    {
                        //Extracts the url from the "src" attribute.
                        var value = artistImageSrcValue.Value.Substring(artistImageSrcValue.Value.IndexOf("\"")).Trim('\"');
                        if (value.StartsWith("//"))
                            value = "http:" + value;

                        //Assigns the url to the JPopAsiaArtistData object.
                        result.ArtistImageUrl = new Uri(value);
                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
                //Clean up of Http stuff.
                if (httpResponse != null)
                {
                    httpResponse.Dispose();
                    httpResponse = null;
                }
            }

            //todo cache

            return result;
        }
    }

    /// <summary>
    /// An object representing an artist listed in BuiltinArtists.xml.
    /// </summary>
    public class BuiltinArtistEntry
    {
        /// <summary>
        /// The name of the artist.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// The URL of their JPopAsiaUrl page, if they have one.
        /// </summary>
        public Uri JPopAsiaUrl { get; set; }
        /// <summary>
        /// A list of alternative names that the artist has.
        /// </summary>
        public BuiltinArtistEntryAltName[] AltNames { get; set; }
        /// <summary>
        /// The URL of their FanArtTVUrl page, if they have one.
        /// </summary>
        public Uri FanArtTVUrl { get; set; }
        /// <summary>
        /// The country of origin for the artist.
        /// </summary>
        public string CountryOfOrigin { get; internal set; }
        /// <summary>
        /// The language the artist's name is in.
        /// </summary>
        public string NameLanguage { get; internal set; } = "en";
        /// <summary>
        /// How to pronounce the artist's name, if applicable.
        /// </summary>
        public string NameSayAs { get; internal set; }
        public Uri MusicBrainzUrl { get; internal set; }
    }

    /// <summary>
    /// An object representing an alternative way to refer to an artist.
    /// </summary>
    public struct BuiltinArtistEntryAltName
    {
        public BuiltinArtistEntryAltName(string name, string lang = "en", string sayAs = null)
        {
            NameLanguage = lang;
            Name = name;
            NameSayAs = sayAs;
        }

        /// <summary>
        /// The alternative name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The language the name is in.
        /// </summary>
        public string NameLanguage { get; private set; }
        /// <summary>
        /// How to pronounce the name, if applicable.
        /// </summary>
        public string NameSayAs { get; private set; }
    }

    /// <summary>
    /// An object representing the data received for an artist on JPopAsia.com
    /// </summary>
    public class JPopAsiaArtistData
    {
        /// <summary>
        /// The URL to the image of the artist on the website.
        /// </summary>
        public Uri ArtistImageUrl { get; internal set; }
        /// <summary>
        /// The artist's name.
        /// </summary>
        public string ArtistName { get; internal set; }
    }
}
