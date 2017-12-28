using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.Web.Http;
using static Neptunium.NepApp;

namespace Neptunium.Core.Stations
{
    public class NepAppStationsManager : INepAppFunctionManager
    {
        private const string StationsFilePath = @"Data\Stations\Data\Stations.xml";
        internal NepAppStationsManager()
        {
            if (!ApplicationData.Current.RoamingSettings.Values.ContainsKey(nameof(LastPlayedStationName)))
            {
                ApplicationData.Current.RoamingSettings.Values.Add(new KeyValuePair<string, object>(nameof(LastPlayedStationName), null));
            }
            else
            {
                LastPlayedStationName = (string)ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationName)];
            }
        }

        internal void SetLastPlayedStationName(string value)
        {
            LastPlayedStationName = value;
            ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationName)] = value;
        }

        public string LastPlayedStationName { get; private set; }

        internal async Task<StationItem[]> GetStationsAsync()
        {

            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(StationsFilePath);
            var reader = await file.OpenReadAsync();

            XDocument xmlDoc = XDocument.Load(reader.AsStream());

            try
            {
                List<StationItem> stationList = new List<StationItem>();

                foreach (var stationElement in xmlDoc.Element("Stations").Elements("Station"))
                {
                    var streams = stationElement.Element("Streams").Elements("Stream").Select<XElement, StationStream>(x =>
                    {
                        var stream = new StationStream(new Uri(x.Value));
                        stream.ContentType = x.Attribute("ContentType")?.Value;

                        if (x.Attribute("Bitrate") != null)
                            stream.Bitrate = int.Parse(x.Attribute("Bitrate")?.Value);

                        if (x.Attribute("RelativePath") != null)
                            stream.RelativePath = x.Attribute("RelativePath")?.Value;

                        if (x.Attribute("ServerType") != null)
                        {
                            stream.ServerFormat = (StationStreamServerFormat)Enum.Parse(typeof(StationStreamServerFormat), x.Attribute("ServerType").Value);
                        }

                        if (x.Attribute("RequestMetadata") != null)
                        {
                            stream.RequestMetadata = bool.Parse(x.Attribute("RequestMetadata").Value);
                        }
                        else
                        {
                            //defaults to true
                            stream.RequestMetadata = true;
                        }

                        return stream;
                    }).ToArray();

                    var stationLogoUri = await CacheStationLogoUriAsync(new Uri(stationElement.Element("Logo").Value));

                    var station = new StationItem(
                        name: stationElement.Element("Name").Value,
                        description: stationElement.Element("Description").Value,
                        stationLogo: stationLogoUri,
                        streams: streams);

                    station.StationLogoUrlOnline = new Uri(stationElement.Element("Logo").Value);

                    if (stationElement.Element("Programs") != null)
                    {
                        station.Programs = stationElement.Element("Programs").Elements("Program").Select<XElement, StationProgram>(x =>
                        {

                            var program = new StationProgram();
                            program.Name = x.Attribute("Name")?.Value;
                            program.Station = station;

                            if (x.Attribute("Style") != null)
                            {
                                program.Style = (StationProgramStyle)Enum.Parse(typeof(StationProgramStyle), x.Attribute("Style").Value);
                            }
                            else
                            {
                                program.Style = StationProgramStyle.Hosted;
                            }

                            if (program.Style == StationProgramStyle.Hosted)
                            {
                                //hosted programs rely on the "artist" string (from song metadata) to match in order to activate.

                                program.Host = x.Attribute("Host").Value;
                                program.HostRegexExpression = x.Attribute("HostExp")?.Value;
                            }
                            else if (program.Style == StationProgramStyle.Block)
                            {
                                //block programs are activated based on the time and the current station.
                            }

                            if (x.HasElements)
                            {
                                if (x.Elements("Listing") != null)
                                {
                                    //if the program has an actual schedule (e.g. always occur on a certain day around a certain time), grab its time listings.

                                    var listings = new List<StationProgramTimeListing>();

                                    foreach (var listingElement in x.Elements("Listing"))
                                    {
                                        try
                                        {
                                            StationProgramTimeListing listing = new StationProgramTimeListing();
                                            listing.Day = listingElement.Attribute("Day").Value;
                                            listing.Time = DateTime.Parse(listingElement.Attribute("Time").Value);

                                            if (listingElement.Attribute("EndTime") != null)
                                            {
                                                listing.EndTime = DateTime.Parse(listingElement.Attribute("Time").Value);
                                            }

                                            listings.Add(listing);
                                        }
                                        catch (Exception ex)
                                        {
#if !DEBUG
                                            Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex);
#endif
                                        }
                                    }

                                    program.TimeListings = listings.ToArray();
                                }
                            }

                            return program;
                        }).ToArray();
                    }
                    else
                    {
                        station.Programs = null;
                    }

                    if (stationElement.Element("Background") != null)
                    {
                        var backgroundElement = stationElement.Element("Background");

                        station.Background = backgroundElement.Value;
                    }

                    station.Site = stationElement.Element("Site").Value;
                    station.Genres = stationElement.Element("Genres").Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    station.PrimaryLocale = stationElement.Element("PrimaryLocale")?.Value;

                    if (stationElement.Element("StationMessages") == null)
                    {
                        station.StationMessages = new string[] { };
                    }
                    else
                    {
                        var stationMsgElement = stationElement.Element("StationMessages");
                        var messages = stationMsgElement.Elements("Message").Select(x => x.Value.ToString()).ToArray();
                        station.StationMessages = messages;
                    }

                    station.Group = stationElement.Element("StationGroup")?.Value;

                    stationList.Add(station);

                }

                return stationList.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred", ex);
            }
            finally
            {
                xmlDoc = null;
                reader.Dispose();
            }
        }

        public async Task<Uri> CacheStationLogoUriAsync(Uri uri)
        {
            //this method takes the online station uri and redirects it to a local copy (and caches it locally if it hasn't already).


            var imageCacheFolder = await NepApp.ImageCacheFolder.CreateFolderAsync("StationLogos", CreationCollisionOption.OpenIfExists);

            var originalFileName = uri.Segments.Last().Trim();

            StorageFile fileObject = await imageCacheFolder.TryGetItemAsync(originalFileName) as StorageFile;

            if (fileObject == null)
            {
                if (!NepApp.Network.IsConnected)
                {
                    return uri; //return the online uri for now.
                }
                else
                {
                    //cache the station logo for offline use.
                    fileObject = await imageCacheFolder.CreateFileAsync(originalFileName);
                    Stream fileStream = await fileObject.OpenStreamForWriteAsync(); //auto disposed by the using statement on the next line
                    using (IOutputStream outputFileStream = fileStream.AsOutputStream())
                    {
                        using (HttpClient http = new HttpClient())
                        {
                            var httpResponse = await http.GetAsync(uri);
                            await httpResponse.Content.WriteToStreamAsync(outputFileStream);
                            await outputFileStream.FlushAsync();
                            httpResponse.Dispose();
                        }
                    }


                    //falls through below where it returns our cached copy.
                }
            }

            //return our local copy.

            return new Uri(fileObject.Path);
        }



        public async Task<Uri> GetCachedStationLogoRelativeUriAsync(StationItem station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            var tileCacheFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("StationLogosForTiles", CreationCollisionOption.OpenIfExists);

            Uri result = null;

            var originalFileName = station.StationLogoUrlOnline.Segments.Last().Trim();

            StorageFile fileObject = await tileCacheFolder.TryGetItemAsync(originalFileName) as StorageFile;

            if (fileObject == null)
            {
                //cache the station logo for offline use.
                fileObject = await tileCacheFolder.CreateFileAsync(originalFileName);
                Stream fileStream = await fileObject.OpenStreamForWriteAsync(); //auto disposed by the using statement on the next line
                using (IOutputStream outputFileStream = fileStream.AsOutputStream())
                {
                    using (HttpClient http = new HttpClient())
                    {
                        var httpResponse = await http.GetAsync(station.StationLogoUrlOnline);
                        await httpResponse.Content.WriteToStreamAsync(outputFileStream);
                        await outputFileStream.FlushAsync();
                        httpResponse.Dispose();
                    }
                }
            }

            result = new Uri(fileObject.Path);

            if (result != null)
            {
                string fileName = result.Segments.Last();
                var cached = new Uri("ms-appx:///StationLogosForTiles/" + fileName);
                return cached;
            }

            return null;
        }

        internal async Task<StationItem> GetStationByNameAsync(string stationPlayedOn)
        {
            //ugly way to do this
            var station = (await GetStationsAsync()).First(x => x.Name.Equals(stationPlayedOn));
            return station;
        }
    }
}
