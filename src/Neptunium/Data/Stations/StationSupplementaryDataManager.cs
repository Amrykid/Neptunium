using Kukkii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.Web.Http;

namespace Neptunium.Data.Stations
{
    public static class StationSupplementaryDataManager
    {
        public static async Task<Color> GetStationLogoDominantColorAsync(StationModel station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            string colorKey = "LogoColor|" + station.Name;

            if (await CookieJar.DeviceCache.ContainsObjectAsync(colorKey))
                return await CookieJar.DeviceCache.PeekObjectAsync<Color>(colorKey);

            var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(station.Logo));
            var stationLogoStream = await streamRef.OpenReadAsync();
            var color = await ColorUtilities.GetDominantColorAsync(stationLogoStream);

            stationLogoStream.Dispose();

            await CookieJar.DeviceCache.InsertObjectAsync<Color>(colorKey, color);

            return color;
        }

        public static async Task<Uri> GetCachedStationLogoUriAsync(StationModel station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            Uri webUrl = new Uri(station.Logo);
            string fileName = webUrl.Segments.Last();
            var localFileRef = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            if (localFileRef != null)
            {
                return new Uri(localFileRef.Path);
            }
            else
            {
                using (HttpClient http = new HttpClient())
                {
                    var data = await http.GetBufferAsync(webUrl);

                    var createdFileRef = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName);
                    using (var stream = await createdFileRef.OpenAsync(FileAccessMode.ReadWrite))
                        await stream.WriteAsync(data);

                    return new Uri(createdFileRef.Path);
                }
            }
        }

        public static async Task<Uri> GetCachedStationLogoRelativeUriAsync(StationModel station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            var url = await GetCachedStationLogoUriAsync(station);

            if (url != null)
            {
                string path = "ms-appdata://localfolder/";
                string fileName = url.Segments.Last();
                path += fileName;

                return new Uri(path);
            }

            return null;
        }
    }
}
