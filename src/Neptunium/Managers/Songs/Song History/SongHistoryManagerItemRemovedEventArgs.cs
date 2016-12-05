using System;

namespace Neptunium.Managers
{
    public class SongHistoryManagerItemRemovedEventArgs: EventArgs
    {
        internal SongHistoryManagerItemRemovedEventArgs()
        {

        }

        public SongHistoryItem RemovedItem { get; internal set; }
    }
}