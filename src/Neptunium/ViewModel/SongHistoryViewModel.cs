using Crystal3.Model;
using Neptunium.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;

namespace Neptunium.ViewModel
{
    public class SongHistoryViewModel: ViewModelBase
    {

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            SongHistory = SongHistoryManager.SongHistory;
        }

        public ReadOnlyObservableCollection<SongHistoryItem> SongHistory
        {
            get { return GetPropertyValue<ReadOnlyObservableCollection<SongHistoryItem>>(); }
            set { SetPropertyValue<ReadOnlyObservableCollection<SongHistoryItem>>(value: value); }
        }
    }
}
