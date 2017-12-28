using Kukkii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

namespace Neptunium.Core.Stations
{
    public static class StationSupplementaryDataManager
    {
        public static async Task<Color> GetStationLogoDominantColorAsync(StationItem station)
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
                var streamRef = RandomAccessStreamReference.CreateFromUri(station.StationLogoUrlOnline);
                stationLogoStream = await streamRef.OpenReadAsync();
                color = await ColorUtilities.GetDominantColorAsync(stationLogoStream);
            }
            catch (Exception ex)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Station Name", station.Name);
                data.Add("Station Logo URL", station.StationLogoUrlOnline?.ToString());
                data.Add("Station URL", station.Site);

                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, data);
            }
            finally
            {
                stationLogoStream?.Dispose();
            }

            await CookieJar.DeviceCache.PushObjectAsync<string>(colorKey, color.ToString());

            return color;
        }

        public static async Task<Color> GetStationBackgroundDominantColorAsync(StationItem station)
        {
            if (station == null) throw new ArgumentNullException(nameof(station));

            if (station.Background == null) return Colors.Transparent;

            string colorKey = "BgColor|" + station.Name;
            Color color = default(Color);

            if (await CookieJar.DeviceCache.ContainsObjectAsync(colorKey))
            {
                var hexCode = await CookieJar.DeviceCache.PeekObjectAsync<string>(colorKey);

                return ColorUtilities.ParseFromHexString(hexCode);
            }

            IRandomAccessStreamWithContentType stationBgStream = null;
            try
            {
                var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(station.Background));
                stationBgStream = await streamRef.OpenReadAsync();
                color = await ColorUtilities.GetDominantColorAsync(stationBgStream);
            }
            catch (Exception ex)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Station Name", station.Name);
                data.Add("Station Background URL", station.Background?.ToString());
                data.Add("Station URL", station.Site);

                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, data);
            }
            finally
            {
                stationBgStream?.Dispose();
            }

            await CookieJar.DeviceCache.PushObjectAsync<string>(colorKey, color.ToString());

            return color;
        }

        public static async Task<Color> GetDominantColorAsync(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            Color color = default(Color);

            IRandomAccessStreamWithContentType stationBgStream = null;
            try
            {
                var streamRef = RandomAccessStreamReference.CreateFromUri(uri);
                stationBgStream = await streamRef.OpenReadAsync();
                color = await ColorUtilities.GetDominantColorAsync(stationBgStream);
            }
            catch (Exception ex)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Background URI", uri.ToString());

                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, data);
            }
            finally
            {
                stationBgStream?.Dispose();
            }

            return color;
        }
    }

}
