using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Threading;

namespace Neptunium.Data
{
    public static class StationDataManager
    {
        public static bool IsInitialized { get; private set; }

        public static IEnumerable<StationModel> Stations { get; private set; }

        private static SemaphoreSlim initLock = new SemaphoreSlim(1);
        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            await initLock.WaitAsync();

            if (IsInitialized) return;

            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Data\Stations\Stations.xml");
            var reader = await file.OpenReadAsync();
            XDocument xmlDoc = XDocument.Load(reader.AsStream());

            List<StationModel> stationList = new List<StationModel>();

            foreach (var stationElement in xmlDoc.Element("Stations").Elements("Station"))
            {
                var station = new StationModel();

                station.Name = stationElement.Element("Name").Value;
                station.Description = stationElement.Element("Description").Value;
                station.Logo = stationElement.Element("Logo").Value;

                try
                {
                    if (stationElement.Element("Background") != null)
                    {
                        var backgroundElement = stationElement.Element("Background");
                        station.Background = backgroundElement.Value;
                    }
                }
                catch (Exception) { }

                station.Site = stationElement.Element("Site").Value;
                station.Genres = stationElement.Element("Genres").Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                station.PrimaryLocale = stationElement.Element("PrimaryLocale")?.Value;

                station.Streams = stationElement.Element("Streams").Elements("Stream").Select<XElement, StationModelStream>(x =>
                {
                    var stream = new StationModelStream();

                    stream.ContentType = x.Attribute("ContentType")?.Value;
                    stream.Bitrate = int.Parse(x.Attribute("Bitrate")?.Value);
                    stream.SampleRate = uint.Parse(x.Attribute("SampleRate")?.Value);
                    stream.Url = x.Value;
                    stream.RelativePath = x.Attribute("RelativePath")?.Value;

                    try
                    {
                        if (x.Attribute("ServerType") != null)
                        {
                            stream.ServerType = (StationModelStreamServerType)Enum.Parse(typeof(StationModelStreamServerType), x.Attribute("ServerType").Value);

                            stream.HistoryPath = x.Attribute("HistoryPath")?.Value;
                        }
                    }
                    catch (Exception) { }

                    return stream;
                }).ToArray();

                try
                {
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
                }
                catch (Exception) { }

                stationList.Add(station);
            }

            Stations = stationList.OrderBy(x => x.Name).ToArray();

            IsInitialized = true;

            stationList = null;

            xmlDoc = null;

            reader.Dispose();

            initLock.Release();

        }

        internal static Task DeinitializeAsync()
        {
            if (!IsInitialized) return Task.CompletedTask;

            Stations = null;

            IsInitialized = false;

            return Task.CompletedTask;
        }
    }
}
