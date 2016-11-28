using Kukkii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

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
    }
}
