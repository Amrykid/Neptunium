using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Neptunium.NepApp;

namespace Neptunium.Core.Stations
{
    public class NepAppStationsManager : INepAppFunctionManager
    {
        private const string StationsFilePath = @"Data\Stations\Data\Stations.xml";
        internal NepAppStationsManager()
        {

        }

        internal async Task<StationItem[]> GetStationsAsync()
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(StationsFilePath);
            var reader = await file.OpenReadAsync();

            XDocument xmlDoc = XDocument.Load(reader.AsStream());
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

                var station = new StationItem(
                    name: stationElement.Element("Name").Value,
                    description: stationElement.Element("Description").Value,
                    stationLogo: new Uri(stationElement.Element("Logo").Value),
                    streams: streams);

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

                stationList.Add(station);

            }
            xmlDoc = null;
            reader.Dispose();

            return stationList.ToArray();
        }

        internal async Task<StationItem> GetStationByNameAsync(string stationPlayedOn)
        {
            //ugly way to do this
            var station = (await GetStationsAsync()).First(x => x.Name.Equals(stationPlayedOn));
            return station;
        }
    }
}
