namespace Neptunium.Core.Media.History
{
    public class SongHistorianSongUpdatedEventArgs
    {
        private SongHistoryItem item;

        internal SongHistorianSongUpdatedEventArgs(SongHistoryItem item)
        {
            this.item = item;
        }

        public SongHistoryItem Item { get { return this.item; } }
    }
}