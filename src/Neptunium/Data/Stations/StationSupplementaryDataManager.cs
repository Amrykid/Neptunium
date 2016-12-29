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
            {
                var hexCode = await CookieJar.DeviceCache.PeekObjectAsync<string>(colorKey);

                return ColorUtilities.ParseFromHexString(hexCode);
            }

            var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(station.Logo));
            var stationLogoStream = await streamRef.OpenReadAsync();
            var color = await ColorUtilities.GetDominantColorAsync(stationLogoStream);

            stationLogoStream.Dispose();

            await CookieJar.DeviceCache.InsertObjectAsync<string>(colorKey, color.ToString());

            return color;
        }
    }
}
