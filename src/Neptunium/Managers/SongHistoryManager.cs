using Kukkii;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers
{
    public static class SongHistoryManager
    {
        public static bool IsInitialized { get; private set; }

        public static ReadOnlyObservableCollection<SongHistoryItem> SongHistory { get; private set; }
        private static ObservableCollection<SongHistoryItem> songHistoryCollection = null;

        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;
            SongMetadataManager.FoundMetadata += SongMetadataManager_FoundMetadata;

            songHistoryCollection = await CookieJar.DeviceCache.GetObjectAsync<ObservableCollection<SongHistoryItem>>("SongHistory", () => new ObservableCollection<SongHistoryItem>());
            SongHistory = new ReadOnlyObservableCollection<SongHistoryItem>(songHistoryCollection);

            IsInitialized = true;

            await Task.CompletedTask;
        }

        public static async Task FlushAsync()
        {
            await CookieJar.DeviceCache.UpdateObjectAsync<ObservableCollection<SongHistoryItem>>("SongHistory", songHistoryCollection);
            await CookieJar.DeviceCache.FlushAsync();
        }

        private static async void SongMetadataManager_FoundMetadata(object sender, SongMetadataManagerFoundMetadataEventArgs e)
        {
            if (songHistoryCollection.Any(x => x.Artist == e.QueiredArtist && x.Track == e.QueriedTrack))
            {
                var item = songHistoryCollection.First(x => x.Artist == e.QueiredArtist && x.Track == e.QueriedTrack);
                var index = songHistoryCollection.IndexOf(item);

                item.Album = e.FoundAlbumData;

                songHistoryCollection[index] = item;

                await FlushAsync();
            }
        }

        private static void StationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Any(x => x == e.Title)) return;
            //add a new song to the metadata when the song changes.

            var historyItem = new SongHistoryItem();
            historyItem.Track = e.Title;
            historyItem.Artist = e.Artist;
            historyItem.Station = StationMediaPlayer.CurrentStation.Name;
            historyItem.DatePlayed = DateTime.Now;

            songHistoryCollection.Add(historyItem);
        }
    }
}
