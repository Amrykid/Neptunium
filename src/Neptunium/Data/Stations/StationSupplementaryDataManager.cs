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
            Color color = default(Color);

            if (await CookieJar.DeviceCache.ContainsObjectAsync(colorKey))
            {
                var hexCode = await CookieJar.DeviceCache.PeekObjectAsync<string>(colorKey);

                return ColorUtilities.ParseFromHexString(hexCode);
            }

            IRandomAccessStreamWithContentType stationLogoStream = null;
            try
            {
                var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(station.Logo));
                stationLogoStream = await streamRef.OpenReadAsync();
                color = await ColorUtilities.GetDominantColorAsync(stationLogoStream);
            }
            catch (Exception ex)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Station Name", station.Name);
                data.Add("Station Logo URL", station.Logo);
                data.Add("Station URL", station.Site);

                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, data);
            }
            finally
            {
                stationLogoStream?.Dispose();
            }

            await CookieJar.DeviceCache.InsertObjectAsync<string>(colorKey, color.ToString());

            return color;
        }
    }
}
