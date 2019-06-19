using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using static Neptunium.NepApp;

namespace Neptunium.Core.Media.Metadata
{
    public class NepAppMetadataManager : INepAppFunctionManager
    {
        private StorageFile builtInArtistsFile = null;
        private BuiltinArtistEntry[] builtinArtistEntries = null;
        //Defines our musicbrainz metadata source and place holder objects.
        private MusicBrainzMetadataSource musicBrainzSource = null;
        private Regex featuredArtistRegex = new Regex(@"(?:(?:f|F)(?:ea)*t(?:uring)*\.?\s*(.+)(?:\n|$))");

        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            builtInArtistsFile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Data\Artists\Neptunium-ArtistsDB\BuiltinArtists.xml");
            builtinArtistEntries = await LoadBuiltinArtistEntriesAsync();
            musicBrainzSource = new MusicBrainzMetadataSource();
            IsInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of artists from BuiltinArtists.xml
        /// </summary>
        /// <returns>A list of artists.</returns>
        private async Task<BuiltinArtistEntry[]> LoadBuiltinArtistEntriesAsync()
        {
            XDocument xmlDoc = null;
            IRandomAccessStreamWithContentType reader = null; //local file only

            try
            {
                using (HttpClient http = new HttpClient())
                {
                    var xmlText = await http.GetStringAsync("https://raw.githubusercontent.com/Amrykid/Neptunium-ArtistsDB/master/BuiltinArtists.xml");
                    xmlDoc = XDocument.Parse(xmlText);
                }
            }
            catch (Exception)
            {
                //Opens up an XDocument for reading the xml file.

                var file = builtInArtistsFile;
                reader = await file.OpenReadAsync();
                xmlDoc = XDocument.Load(reader.AsStream());
            }

            //Creates a list to hold the artists.
            List<BuiltinArtistEntry> artists = new List<BuiltinArtistEntry>();

            //Looks through the <Artist> elements in the file, creating a BuiltinArtistEntry for each one.
            foreach (var artistElement in xmlDoc.Element("Artists").Elements("Artist"))
            {
                var artistEntry = new BuiltinArtistEntry();

                artistEntry.Name = artistElement.Attribute("Name").Value;

                if (artistElement.Attribute("JPopAsiaUrl") != null)
                    artistEntry.JPopAsiaUrl = new Uri(artistElement.Attribute("JPopAsiaUrl").Value);

                if (artistElement.Elements("AltName") != null)
                {
                    artistEntry.AltNames = artistElement.Elements("AltName").Select(altNameElement =>
                    {
                        string name = altNameElement.Value;
                        string lang = "en";
                        string sayAs = null;

                        if (altNameElement.Attribute("Lang") != null)
                            lang = altNameElement.Attribute("Lang").Value;

                        if (altNameElement.Attribute("SayAs") != null)
                            sayAs = altNameElement.Attribute("SayAs").Value;

                        return new BuiltinArtistEntryAltName(name, lang, sayAs);
                    }).ToArray();
                }

                if (artistElement.Elements("Related") != null)
                {
                    artistEntry.RelatedArtists = artistElement.Elements("Related").Select(relatedElement =>
                    {
                        return relatedElement.Value;
                    }).ToArray();
                }

                if (artistElement.Attribute("FanArtTVUrl") != null)
                {
                    artistEntry.FanArtTVUrl = new Uri(artistElement.Attribute("FanArtTVUrl").Value);
                }

                if (artistElement.Attribute("MusicBrainzUrl") != null)
                {
                    artistEntry.MusicBrainzUrl = new Uri(artistElement.Attribute("MusicBrainzUrl").Value);
                }

                if (artistElement.Attribute("OriginCountry") != null)
                {
                    artistEntry.CountryOfOrigin = artistElement.Attribute("OriginCountry").Value;
                }

                if (artistElement.Attribute("NameLanguage") != null)
                {
                    artistEntry.NameLanguage = artistElement.Attribute("NameLanguage").Value;
                }
                else
                {
                    artistEntry.NameLanguage = "en";
                }

                if (artistElement.Attribute("SayAs") != null)
                {
                    artistEntry.NameSayAs = artistElement.Attribute("SayAs").Value;
                }

                //Adds the artist entry to the list.
                artists.Add(artistEntry);
            }

            //Clean up.
            xmlDoc = null;
            reader?.Dispose();

            return artists.ToArray();
        }

        public BuiltinArtistEntry FindBuiltInArtist(string artistName, string stationLocale)
        {
            if (!IsInitialized) return null;

            //Try and find an entry with the same name as the artist.
            BuiltinArtistEntry builtInMatch = builtinArtistEntries.FirstOrDefault(x => x.Name.ToLower().Equals(artistName.ToLower()));

            if (builtInMatch == null) //If we don't find a direct match, we'll need to do some digging.
            {
                builtInMatch = builtinArtistEntries.FirstOrDefault(x =>
                {
                    //Figure out if we should check using locale as an additional test case. If the country of origin (on the artist) or station locale (on the station) isn't defined, we just return true.
                    bool countryLocaleMatches = (!string.IsNullOrWhiteSpace(x.CountryOfOrigin) && !string.IsNullOrWhiteSpace(stationLocale) ? x.CountryOfOrigin.Equals(stationLocale) : true);


                    //string lastNameFirstNameSwappedName = string.Join(" ", artistName.Split(' ').Reverse()); //splices, reverses and joins: "Ayumi Hamasaki" -> ["Ayumi","Hamasaki"] -> ["Hamasaki", "Ayumi"] -> "Hamasaki Ayumi"

                    //Checks all alternative names listed for the artist to see if they roughly match.
                    return x.AltNames != null ? (x.AltNames.Any(y => y.Name.ToLower().Equals(artistName.ToLower())) && countryLocaleMatches) : false;
                });
            }

            return builtInMatch;
        }

        private BuiltinArtistEntry FindBuiltInArtistInsideInArtistMetadata(string artist, string primaryLocale)
        {
            if (!IsInitialized) return null;
            if (string.IsNullOrWhiteSpace(artist)) return null;

            BuiltinArtistEntry result = null;

            foreach(BuiltinArtistEntry builtinArtistEntry in builtinArtistEntries)
            {
                if (artist.Contains("(" + builtinArtistEntry.Name + ")"))
                {
                    result = builtinArtistEntry;
                    break;
                }

                if (builtinArtistEntry.AltNames != null)
                {
                    if (builtinArtistEntry.AltNames.Any(x => artist.Contains("(" + x.Name + ")")))
                    {
                        result = builtinArtistEntry;
                        break;
                    }
                }
            }

            return result;
        }

        public async Task<bool> FindAdditionalMetadataAsync(ExtendedSongMetadata originalMetadata)
        {
            if (!IsInitialized) return false;

            //Checks if original metadata is null.
            if (originalMetadata == null) throw new ArgumentNullException(nameof(originalMetadata));
            //Checks if Unknown Metadata was passed ("Unknown Song - Unknown Artist").
            if (originalMetadata.IsUnknownMetadata) throw new ArgumentException("Unknown metadata was passed.", nameof(originalMetadata));

            //Checks if we're on battery saver mode.
            if (Windows.System.Power.PowerManager.EnergySaverStatus == Windows.System.Power.EnergySaverStatus.On)
                return false;

            //We're not on battery saver mode. Let's check to see if we're on a metered network or roaming.
            if (NepApp.Network.NetworkUtilizationBehavior != NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.Normal)
                return false;

            //We're not metered or roaming. Lastly, lets check if the user wants us to find song metadata.
            if (!(bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata))
                return false;


            AlbumData albumData = null;
            ArtistData artistData = null;
            var station = await NepApp.Stations.GetStationByNameAsync(originalMetadata.StationPlayedOn);

            ExtractFeaturedArtists(originalMetadata);

            //First, grab album data from musicbrainz.
            albumData = await musicBrainzSource.TryFindAlbumAsync(originalMetadata.Track, originalMetadata.Artist, station.PrimaryLocale);

            //Next, try and figure out the artist
            var builtInArtist = FindBuiltInArtist(originalMetadata.Artist, station.PrimaryLocale);
            if (builtInArtist == null) builtInArtist = FindBuiltInArtistInsideInArtistMetadata(originalMetadata.Artist, station.PrimaryLocale);
            if (builtInArtist != null)
            {
                if (builtInArtist.MusicBrainzUrl != null)
                {
                    //Grab MusicBrainz information directly from the url.

                    string artistID = musicBrainzSource.ExtractIDFromUri(builtInArtist.MusicBrainzUrl);
                    artistData = await musicBrainzSource.GetArtistAsync(artistID, builtInArtist.CountryOfOrigin ?? "JP");
                }

                if (builtInArtist.JPopAsiaUrl != null)
                {
                    originalMetadata.JPopAsiaArtistInfo = await JPopAsiaArtistFetcher.GetArtistDataOnJPopAsiaAsync(builtInArtist.Name, builtInArtist.JPopAsiaUrl);
                }
            }
            else
            {
                await Task.Delay(500); //500 ms sleep

                //Next, try and grab artist data from musicbrainz.
                artistData = await musicBrainzSource.TryFindArtistAsync(originalMetadata.Artist, station.PrimaryLocale);

                //Grab information about the artist from JPopAsia.com
                originalMetadata.JPopAsiaArtistInfo = await JPopAsiaArtistFetcher.FindArtistDataOnJPopAsiaAsync(originalMetadata.Artist.Trim(), station.PrimaryLocale);
            }

            //Grab a background of the artist from FanArtTV.com
            originalMetadata.FanArtTVBackgroundUrl = await FanArtTVFetcher.FetchArtistBackgroundAsync(originalMetadata.Artist.Trim(), station.PrimaryLocale);


            //todo cache
            originalMetadata.Album = albumData;
            originalMetadata.ArtistInfo = artistData;

            return true;
        }

        private void ExtractFeaturedArtists(ExtendedSongMetadata originalMetadata)
        {
            //try to strip out "Feat." artists

            string originalArtistString = originalMetadata.Artist;
            if (featuredArtistRegex.IsMatch(originalMetadata.Artist))
                originalMetadata.Artist = featuredArtistRegex.Replace(originalMetadata.Artist, "").Trim();

            if (featuredArtistRegex.IsMatch(originalArtistString))
            {
                try
                {
                    var artistsMatch = featuredArtistRegex.Match(originalArtistString);
                    var artists = artistsMatch.Groups[1].Value.Split(',').Select(x => x.Trim());
                    originalMetadata.FeaturedArtists = artists.ToArray();
                }
                catch (Exception)
                {

                }
            }
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
        public string[] RelatedArtists { get; internal set; }
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
}
