using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Core.Media.Metadata;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.IO;
using Windows.Storage;
using Crystal3.Utilities;

namespace Neptunium.Core.Media.History
{
    public class SongHistorian
    {
        private JsonSerializer serializer = null;
        private StorageFolder dataFolder = null;
        internal SongHistorian()
        {
            HistoryOfSongs = new ObservableCollection<SongHistoryItem>();

            serializer = new JsonSerializer();
        }
        internal async Task InitializeAsync()
        {
            dataFolder = await CreateAndReturnDataDirectoryAsync();

            StorageFile historyFile = null;
            if ((historyFile = await dataFolder.GetFileAsync("History.json")) != null)
            {
                var accessStream = await historyFile.OpenReadAsync();
                byte[] data = null;
                using (Stream stream = accessStream.AsStreamForRead())
                {
                    data = new byte[(int)stream.Length];
                    await stream.ReadAsync(data, 0, (int)stream.Length);
                }
                using (StringReader sr = new StringReader(System.Text.UTF8Encoding.UTF8.GetString(data, 0, data.Length)))
                {
                    using (JsonTextReader jtr = new JsonTextReader(sr))
                    {
                        var coll = serializer.Deserialize<ObservableCollection<SongHistoryItem>>(jtr);

                        HistoryOfSongs.AddRange(coll);
                    }
                }
            }

            foreach (SongHistoryItem item in HistoryOfSongs)
            {
                if (item.Metadata.StationLogo != null)
                {
                    if (item.Metadata.StationLogo.Scheme.ToLower().StartsWith("http"))
                        item.Metadata.StationLogo = await NepApp.Stations.CacheStationLogoUriAsync(item.Metadata.StationLogo);
                }
            }
        }

        private async Task<StorageFolder> CreateAndReturnDataDirectoryAsync()
        {
            StorageFolder rootFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
            try
            {
                return await rootFolder.GetFolderAsync("Neptunium");
            }
            catch (Exception) { }

            return await rootFolder.CreateFolderAsync("Neptunium");
        }

        public ObservableCollection<SongHistoryItem> HistoryOfSongs { get; private set; }

        public async Task AddSongAsync(ExtendedSongMetadata newMetadata)
        {
            if (HistoryOfSongs.Count == 30)
            {
                HistoryOfSongs.RemoveAt(0); //remove the latest item from the beginning.
            }

            HistoryOfSongs.Add(new SongHistoryItem() { Metadata = newMetadata, PlayedDate = DateTime.Now });

            StorageFile historyFile = null;
            if ((historyFile = await dataFolder.CreateFileAsync("History.json", CreationCollisionOption.OpenIfExists)) != null)
            {
                string json = null;
                var stream = await historyFile.OpenStreamForWriteAsync();
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonTextWriter jtw = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(jtw, HistoryOfSongs);
                    }
                    json = sw.ToString();
                }
                byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                stream.Dispose();
            }
        }
    }
}
