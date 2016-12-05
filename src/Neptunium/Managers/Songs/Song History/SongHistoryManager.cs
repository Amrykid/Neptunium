using Kukkii;
using Neptunium.Managers;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers
{
    public class SongHistoryManager
    {
        public bool IsInitialized { get; private set; }

        public ReadOnlyObservableCollection<SongHistoryItem> SongHistory { get; private set; }
        private ObservableCollection<SongHistoryItem> songHistoryCollection = null;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;

            songHistoryCollection = await CookieJar.DeviceCache.PeekObjectAsync<ObservableCollection<SongHistoryItem>>("SongHistory", () => new ObservableCollection<SongHistoryItem>());
            SongHistory = new ReadOnlyObservableCollection<SongHistoryItem>(songHistoryCollection);

            var items = SongHistory.ToArray();
            foreach (var item in items.Where(x => x.DatePlayed.AddDays(30) < DateTime.Now)) //remove songs that have been there for longer than 30 days
            {
                songHistoryCollection.Remove(item);
                ItemRemoved?.Invoke(null, new SongHistoryManagerItemRemovedEventArgs() { RemovedItem = item });
            }

            IsInitialized = true;

            await Task.CompletedTask;
        }

        public async Task FlushAsync()
        {
            await CookieJar.DeviceCache.UpdateObjectAsync<ObservableCollection<SongHistoryItem>>("SongHistory", songHistoryCollection);
            await CookieJar.DeviceCache.FlushAsync();
        }


        private async void StationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Any(x => x == e.Title)) return;
            if (e.Artist == "Unknown Artist" && e.Title == "Unknown Song") return;
            //add a new song to the metadata when the song changes.

            var historyItem = new SongHistoryItem();
            historyItem.Track = e.Title;
            historyItem.Artist = e.Artist;
            historyItem.Station = StationMediaPlayer.CurrentStation.Name;
            historyItem.DatePlayed = DateTime.Now;

            songHistoryCollection.Insert(0, historyItem);

            ItemAdded?.Invoke(null, new SongHistoryManagerItemAddedEventArgs() { AddedItem = historyItem });

            await FlushAsync();
        }

        public event EventHandler<SongHistoryManagerItemAddedEventArgs> ItemAdded;
        public event EventHandler<SongHistoryManagerItemRemovedEventArgs> ItemRemoved;
    }
}
