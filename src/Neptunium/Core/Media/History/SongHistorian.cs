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
using System.Reactive.Linq;
using System.Threading;

namespace Neptunium.Core.Media.History
{
    public class SongHistorian
    {
        private JsonSerializer serializer = null;
        private StorageFolder dataFolder = null;
        private StorageFile historyFile = null;

        private SemaphoreSlim historyFileLock = new SemaphoreSlim(1);

        public bool IsInitialized { get; private set; }
        public event EventHandler<SongHistorianSongUpdatedEventArgs> SongAdded;

        internal SongHistorian()
        {
            serializer = new JsonSerializer();
        }
        internal async Task InitializeAsync()
        {
            if (IsInitialized) return;

            dataFolder = NepApp.CacheManager.RoamingDataFilesFolder;
            historyFile = await dataFolder.CreateFileAsync("History.tsv", CreationCollisionOption.OpenIfExists);

            StorageFile oldHistoryFile = await dataFolder.TryGetItemAsync("History.json") as StorageFile;
            if (oldHistoryFile != null) 
            {
                var historyOfSongs = new List<SongHistoryItem>();

                //upgrade to the new history format

                var accessStream = await oldHistoryFile.OpenReadAsync();
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
                        var coll = serializer.Deserialize<List<OldSongHistoryItem>>(jtr);

                        if (coll != null)
                        {
                            foreach (OldSongHistoryItem oldSongHistoryItem in coll)
                            {
                                var newHistoryItem = new SongHistoryItem();
                                newHistoryItem.Artist = oldSongHistoryItem.Metadata.Artist;
                                newHistoryItem.Track = oldSongHistoryItem.Metadata.Track;
                                newHistoryItem.StationPlayedOn = oldSongHistoryItem.Metadata.StationPlayedOn;
                                newHistoryItem.PlayedDate = oldSongHistoryItem.PlayedDate;
                                historyOfSongs.Add(newHistoryItem);
                            }
                        }
                    }
                }

                accessStream.Dispose();

                await oldHistoryFile.DeleteAsync();

                string tsvText = string.Empty;
                foreach(var item in historyOfSongs)
                {
                    tsvText += FormatSongHistoryItemToTSV(item);
                }

                await historyFileLock.WaitAsync();
                try
                {
                    await FileIO.AppendTextAsync(historyFile, tsvText);
                }
                finally
                {
                    historyFileLock.Release();
                }
            }

            IsInitialized = true;
        }

        private string FormatSongHistoryItemToTSV(SongHistoryItem item)
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\n", item.Track, item.Artist, item.StationPlayedOn, item.PlayedDate);
        }


        private SongHistoryItem ParseSongHistoryItemFromTSVLine(string line)
        {
            string strippedLine = line.Trim();
            string[] splice = strippedLine.Split('\t');

            var result = new SongHistoryItem();
            result.Track = splice[0].Trim();
            result.Artist = splice[1].Trim();
            result.StationPlayedOn = splice[2].Trim();
            result.PlayedDate = DateTime.Parse(splice[3].Trim());

            return result;
        }

        public async Task<IEnumerable<SongHistoryItem>> GetHistoryOfSongsAsync()
        {
            if (!IsInitialized) return null;

            List<SongHistoryItem> results = new List<SongHistoryItem>();
            try
            {
                var lines = await FileIO.ReadLinesAsync(historyFile).AsTask().ConfigureAwait(false);

                foreach (var line in lines.Reverse())
                {
                    SongHistoryItem item = ParseSongHistoryItemFromTSVLine(line);

                    results.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return results.AsEnumerable();
        }

        public IObservable<SongHistoryItem> ObserveHistoryOfSongsAsync()
        {
            if (!IsInitialized) Observable.Empty<SongHistoryItem>();
            return Observable.Create<SongHistoryItem>(async o =>
            {
                try
                {
                    var lines = await FileIO.ReadLinesAsync(historyFile);

                    foreach (var line in lines.Reverse())
                    {
                        SongHistoryItem item = ParseSongHistoryItemFromTSVLine(line);

                        o.OnNext(item);
                    }
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }

                o.OnCompleted();
            });
        }

        public async Task AddSongAsync(SongMetadata newMetadata)
        {
            if (!IsInitialized) return;

            if (newMetadata.IsUnknownMetadata) return;

            var item = new SongHistoryItem() { Track = newMetadata.Track, Artist = newMetadata.Artist, StationPlayedOn = newMetadata.StationPlayedOn, PlayedDate = DateTime.Now };

            SongAdded?.Invoke(this, new SongHistorianSongUpdatedEventArgs(item));

            await historyFileLock.WaitAsync();
            try
            {
                await FileIO.AppendTextAsync(historyFile, FormatSongHistoryItemToTSV(item));
            }
            catch (Exception ex)
            {

            }
            finally
            {
                historyFileLock.Release();
            }
        }
    }
}
