using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
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
        private SemaphoreSlim stationsLock = null;
        internal NepAppStationsManager()
        {
            stationsLock = new SemaphoreSlim(1);

            if (!ApplicationData.Current.RoamingSettings.Values.ContainsKey(nameof(LastPlayedStationName)))
            {
                ApplicationData.Current.RoamingSettings.Values.Add(new KeyValuePair<string, object>(nameof(LastPlayedStationName), null));
            }
            else
            {
                LastPlayedStationName = (string)ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationName)];
            }

            if (!ApplicationData.Current.RoamingSettings.Values.ContainsKey(nameof(LastPlayedStationDate)))
            {
                ApplicationData.Current.RoamingSettings.Values.Add(new KeyValuePair<string, object>(nameof(LastPlayedStationDate), null));
            }
            else
            {
                LastPlayedStationDate = DateTime.Parse(ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationDate)].ToString());
            }
        }

        internal void SetLastPlayedStation(string value, DateTime time)
        {
            LastPlayedStationName = value;
            LastPlayedStationDate = time;
            ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationName)] = value;
            ApplicationData.Current.RoamingSettings.Values[nameof(LastPlayedStationDate)] = time.ToString();
        }

        private async Task<StorageFile> GetStationsFileAsync()
        {
            var cachedStationsUri = await NepApp.CacheManager.GetOrCacheUriAsync(NepAppDataCacheManager.CacheType.TextualDataFiles,
                new Uri("https://raw.githubusercontent.com/Amrykid/Neptunium-Stations/master/Stations.xml"), preferOnline: true);
            StorageFile file = null;

            if (cachedStationsUri.Item2 != null)
                file = cachedStationsUri.Item2;
            else
                file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(StationsFilePath);

            return file;
        }

        public string LastPlayedStationName { get; private set; }
        public DateTime LastPlayedStationDate { get; private set; }

        internal async Task<StationItem[]> GetStationsAsync()
        {
            await stationsLock.WaitAsync();

            StorageFile file = await GetStationsFileAsync();

            var reader = await file.OpenReadAsync();

            XDocument xmlDoc = XDocument.Load(reader.AsStream());

            try
            {
                List<StationItem> stationList = new List<StationItem>();

                foreach (var stationElement in xmlDoc.Element("Stations").Elements("Station"))
                {
                    var station = await ConvertStationElementToStationAsync(stationElement);

                    stationList.Add(station);

                }

                stationsLock.Release();
                return stationList.ToArray();
            }
            catch (Exception ex)
            {
                stationsLock.Release();
                throw new Exception("An error occurred", ex);
            }
            finally
            {
                xmlDoc = null;
                reader.Dispose();
            }
        }

        internal IObservable<StationItem> ObserveStationsAsync()
        {
            return Observable.Create<StationItem>(async o =>
            {
                await stationsLock.WaitAsync();

                StorageFile file = await GetStationsFileAsync();

                var reader = await file.OpenReadAsync();

                XDocument xmlDoc = XDocument.Load(reader.AsStream());

                try
                {
                    foreach (var stationElement in xmlDoc.Element("Stations").Elements("Station"))
                    {
                        var station = await ConvertStationElementToStationAsync(stationElement);

                        o.OnNext(station);
                    }

                    stationsLock.Release();
                }
                catch (Exception ex)
                {
                    stationsLock.Release();
                    o.OnError(new Exception("An error occurred", ex));
                }
                finally
                {
                    xmlDoc = null;
                    reader.Dispose();

                    o.OnCompleted();
                }

                return Disposable.Empty;
            });
        }

        private async Task<StationItem> ConvertStationElementToStationAsync(XElement stationElement)
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

            var stationLogoData = await NepApp.CacheManager.GetOrCacheUriAsync(NepAppDataCacheManager.CacheType.StationImages, new Uri(stationElement.Element("Logo").Value));
            Uri stationLogoUri = stationLogoData.Item1;
            if (stationLogoData.Item2 != null)
                stationLogoUri = new Uri(stationLogoData.Item2.Path);


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
                                    listing.Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), listingElement.Attribute("Day").Value);
                                    listing.Time = DateTime.Parse(listingElement.Attribute("Time").Value);

                                    if (listingElement.Attribute("EndTime") != null)
                                    {
                                        listing.EndTime = DateTime.Parse(listingElement.Attribute("EndTime").Value);
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

            return station;
        }

        internal async Task<StationItem> GetStationByNameAsync(string stationPlayedOn)
        {
            //ugly way to do this
            var station = (await GetStationsAsync()).FirstOrDefault(x => x.Name.Equals(stationPlayedOn));
            return station;
        }
    }
}
