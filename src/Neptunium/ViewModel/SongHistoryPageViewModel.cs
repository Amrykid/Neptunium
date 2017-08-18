using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core.Media.History;
using System.Collections.ObjectModel;

namespace Neptunium.ViewModel
{
    public class SongHistoryPageViewModel : ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            History = NepApp.Media.History.HistoryOfSongs.GroupBy(x => x.PlayedDate.Date).OrderBy(x => x.Key).Select(x => x);

            base.OnNavigatedTo(sender, e);
        }

        public IEnumerable<IGrouping<DateTime, SongHistoryItem>> History
        {
            get { return GetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(); }
            private set { SetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(value: value); }
        }
    }
}
