//Re-tooled old code is best old code.

using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neptunium.Data
{
    public static class ShoutcastService
    {
        private static List<string> cached_Shoutcasturls = new List<string>();

        public static async Task<bool> GetIfShoutcastStation(string url)
        {
            try
            {
                var html = await GetHtmlAsync(url);

                var res = html.Contains("SHOUTcast D.N.A.S. Status</font>");

                if (res)
                    cached_Shoutcasturls.Add(url);

                return res;
            }
            catch (Exception)
            {
                return false;
            }

            //return false;
        }

        public static async Task<ObservableCollection<ShoutcastSongHistoryItem>> GetShoutcastStationSongHistoryAsync(StationModel station)
        {
            string url = station.Streams.First().Url;
            var items = await GetShoutcastStationSongHistoryInternalAsync(url);

            var coll = new ObservableCollection<ShoutcastSongHistoryItem>();

            foreach (var item in items)
                coll.Add(new ShoutcastSongHistoryItem() { Time = DateTime.Parse(item.Key), Song = item.Value });


            return coll;
        }
        private static async Task<Dictionary<string, string>> GetShoutcastStationSongHistoryInternalAsync(string url)
        {

            if (url.EndsWith("/") == false)
                url += "/";
            url += "played.html";

            var html = await GetHtmlAsync(url);

            var songtable = Regex.Matches(html, "<table.+?>.+?</table>", RegexOptions.Singleline)[1];
            var entries = Regex.Matches(songtable.Value,
                "<tr>.+?</tr>",
                RegexOptions.Singleline);

            var his = new Dictionary<string, string>();

            await Task.Run(() =>
            {
                for (int i = 1; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var bits = Regex.Matches(
                            Regex.Replace(
                                entry.Value, "<b>Current Song</b>", "", RegexOptions.Singleline),
                        "<td>.+?(</td>|</tr>)", RegexOptions.Singleline);

                    var key = Regex.Replace(bits[0].Value, "<.+?>", "", RegexOptions.Singleline).Trim();
                    var val = Regex.Replace(bits[1].Value, "<.+?>", "", RegexOptions.Singleline).Trim();
                    if (his.ContainsKey(key) == false)
                        his.Add(key,
                            val);
                }
            });

            return his;
        }

        private static async Task<string> GetHtmlAsync(string url)
        {
            using (HttpClient http = new HttpClient())
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");

                var response = await http.SendAsync(httpRequest);

                return await response.Content.ReadAsStringAsync();
            }
        }
    }

    public struct ShoutcastSongHistoryItem
    {
        public DateTime Time { get; set; }
        public string Song { get; set; }

        public string LocalizedTime { get; set; }
    }
}
