using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
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
            //Opens up an XDocument for reading the xml file.
            var file = builtInArtistsFile;
            var reader = await file.OpenReadAsync();
            XDocument xmlDoc = XDocument.Load(reader.AsStream());

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
            reader.Dispose();

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

                    //Last resort, we split the name via a space and try the reverse order. Japanese names are sometimes sent over in "Family-Name First-Name" order instead of "First-Name Family-Name"
                    if (artistName.Contains(" ")) //e.g. "Ayumi Hamasaki" vs. "Hamasaki Ayumi"
                    {
                        //string lastNameFirstNameSwappedName = string.Join(" ", artistName.Split(' ').Reverse()); //splices, reverses and joins: "Ayumi Hamasaki" -> ["Ayumi","Hamasaki"] -> ["Hamasaki", "Ayumi"] -> "Hamasaki Ayumi"

                        //Checks all alternative names listed for the artist to see if they roughly match.
                        return x.AltNames.Any(y => y.Name.ToLower().Equals(artistName.ToLower())) && countryLocaleMatches;
                    }

                    return false;
                });
            }

            return builtInMatch;
        }

        public async Task<bool> FindAdditionalMetadataAsync(ExtendedSongMetadata originalMetadata)
        {
            if (!IsInitialized) return false;

            //Checks if original metadata is null.
            if (originalMetadata == null) throw new ArgumentNullException(nameof(originalMetadata));
            //Checks if Unknown Metadata was passed ("Unknown Song - Unknown Artist").
            if (originalMetadata.IsUnknownMetadata) throw new ArgumentException("Unknown metadata was passed.", nameof(originalMetadata));

            //Checks if we're on battery saver mode.
            if (Windows.System.Power.PowerManager.EnergySaverStatus == Windows.System.Power.EnergySaverStatus.On) return false;

            //We're not on battery saver mode. Let's check to see if we're on a metered network or roaming.
            if (NepApp.Network.NetworkUtilizationBehavior != NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.Normal) return false;

            //We're not metered or roaming. Lastly, lets check if the user wants us to find song metadata.
            if (!(bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata)) return false;


            AlbumData albumData = null;
            ArtistData artistData = null;
            var station = await NepApp.Stations.GetStationByNameAsync(originalMetadata.StationPlayedOn);

            ExtractFeaturedArtists(originalMetadata);

            //First, grab album data from musicbrainz.
            albumData = await musicBrainzSource.TryFindAlbumAsync(originalMetadata.Track, originalMetadata.Artist, station.PrimaryLocale);

            //Next, try and figure out the artist
            var builtInArtist = FindBuiltInArtist(originalMetadata.Artist, station.PrimaryLocale);
            if (builtInArtist != null)
            {
                if (!string.IsNullOrWhiteSpace(builtInArtist.MusicBrainzUrl?.ToString()))
                {
                    //Grab MusicBrainz information directly from the url.

                    string artistID = musicBrainzSource.ExtractIDFromUri(builtInArtist.MusicBrainzUrl);
                    artistData = await musicBrainzSource.GetArtistAsync(artistID, builtInArtist.CountryOfOrigin ?? "JP");
                }
            }
            else
            {
                await Task.Delay(500); //500 ms sleep

                //Next, try and grab artist data from musicbrainz.
                artistData = await musicBrainzSource.TryFindArtistAsync(originalMetadata.Artist, station.PrimaryLocale);

                //Grab information about the artist from JPopAsia.com
                originalMetadata.JPopAsiaArtistInfo = await ArtistFetcher.FindArtistDataOnJPopAsiaAsync(originalMetadata.Artist.Trim(), station.PrimaryLocale);

                //Grab a background of the artist from FanArtTV.com
                originalMetadata.FanArtTVBackgroundUrl = await FanArtTVFetcher.FetchArtistBackgroundAsync(originalMetadata.Artist.Trim(), station.PrimaryLocale);
            }


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
}
