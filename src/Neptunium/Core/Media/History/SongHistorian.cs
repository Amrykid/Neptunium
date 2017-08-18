using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Core.Media.Metadata;
using System.Collections.ObjectModel;

namespace Neptunium.Core.Media.History
{
    public class SongHistorian
    {
        internal SongHistorian()
        {
            HistoryOfSongs = new ObservableCollection<SongHistoryItem>();
        }

        public ObservableCollection<SongHistoryItem> HistoryOfSongs { get; private set; }

        public Task AddSongAsync(ExtendedSongMetadata newMetadata)
        {
            HistoryOfSongs.Add(new SongHistoryItem() { Metadata = newMetadata, PlayedDate = DateTime.Now });

            return Task.CompletedTask;
        }
    }
}
