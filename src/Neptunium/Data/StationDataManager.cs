using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Neptunium.Data
{
    public static class StationDataManager
    {
        public static IEnumerable<StationModel> Stations { get; private set; }

        public static async Task InitializeAsync()
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Data\Stations.xml");
            var reader = await file.OpenReadAsync();
            XDocument xmlDoc = XDocument.Load(reader.AsStream());

            List<StationModel> stationList = new List<StationModel>();

            foreach(var stationElement in xmlDoc.Element("Stations").Elements("Station"))
            {
                var station = new StationModel();

                station.Name = stationElement.Element("Name").Value;
                station.Description = stationElement.Element("Description").Value;
                station.Logo = new Uri(stationElement.Element("Logo").Value);
                station.Site = new Uri(stationElement.Element("Site").Value);
                station.Genres = stationElement.Element("Genres").Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                station.Streams = stationElement.Element("Streams").Elements("Stream").Select<XElement, StationModelStream>(x =>
                {
                    var stream = new StationModelStream();

                    stream.ContentType = x.Attribute("ContentType").Value;
                    stream.Bitrate = int.Parse(x.Attribute("Bitrate").Value);
                    stream.SampleRate = uint.Parse(x.Attribute("SampleRate").Value);
                    stream.Url = x.Value;
                    stream.RelativePath = x.Attribute("RelativePath").Value;

                    return stream;
                });

                stationList.Add(station);
            }

            Stations = stationList.ToArray();
        }
    }
}
