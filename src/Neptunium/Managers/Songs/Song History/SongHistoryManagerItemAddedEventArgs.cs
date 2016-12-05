using System;
namespace Neptunium.Managers
{
    public class SongHistoryManagerItemAddedEventArgs: EventArgs
    {
        internal SongHistoryManagerItemAddedEventArgs()
        {

        }

        public SongHistoryItem AddedItem { get; internal set; }
    }
}