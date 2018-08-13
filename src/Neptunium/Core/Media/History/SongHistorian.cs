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
            dataFolder = NepApp.CacheManager.RoamingDataFilesFolder;

            StorageFile historyFile = null;
            if ((historyFile = await dataFolder.CreateFileAsync("History.json", CreationCollisionOption.OpenIfExists)) != null)
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

                        if (coll != null)
                        {
                            foreach (SongHistoryItem item in coll)
                            {
                                //for song entries that came from another device, the metadata's station logo may point to the wrong file location. we're going to update it for this device.
                                if (!File.Exists(item.Metadata.StationLogo.LocalPath.ToString()))
                                {
                                    item.Metadata.StationLogo = new Uri(NepApp.CacheManager.StationImageCacheFolder.Path + "\\" + item.Metadata.StationLogo.Segments.Last());
                                }
                            }

                            HistoryOfSongs.AddRange(coll);
                        }
                    }
                }
            }

            //todo plug into new caching model
            //foreach (SongHistoryItem item in HistoryOfSongs)
            //{
            //    if (item.Metadata.StationLogo != null)
            //    {
            //        if (item.Metadata.StationLogo.Scheme.ToLower().StartsWith("http"))
            //            item.Metadata.StationLogo = await NepApp.Stations.CacheStationLogoUriAsync(item.Metadata.StationLogo);
            //    }
            //}
        }

        public ObservableCollection<SongHistoryItem> HistoryOfSongs { get; private set; }

        public async Task AddSongAsync(ExtendedSongMetadata newMetadata)
        {
            if (newMetadata.IsUnknownMetadata) return;

            if (HistoryOfSongs.Count == 100)
            {
                HistoryOfSongs.RemoveAt(HistoryOfSongs.Count - 1); //remove the latest item from the end since we're inserting at the beginning.
            }

            HistoryOfSongs.Insert(0, new SongHistoryItem() { Metadata = newMetadata, PlayedDate = DateTime.Now });

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
