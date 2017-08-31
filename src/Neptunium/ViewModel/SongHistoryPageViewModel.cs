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
            NepApp.Media.History.HistoryOfSongs.CollectionChanged += HistoryOfSongs_CollectionChanged;
            UpdateHistory(NepApp.Media.History.HistoryOfSongs);

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Media.History.HistoryOfSongs.CollectionChanged -= HistoryOfSongs_CollectionChanged;

            base.OnNavigatedFrom(sender, e);
        }

        private void HistoryOfSongs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                UpdateHistory(sender as ObservableCollection<SongHistoryItem>);
            });
        }

        private void UpdateHistory(ObservableCollection<SongHistoryItem> collection)
        {
            History = collection.GroupBy(x => x.PlayedDate.Date).OrderByDescending(x => x.Key).Select(x => x);
        }

        public IEnumerable<IGrouping<DateTime, SongHistoryItem>> History
        {
            get { return GetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(); }
            private set { SetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(value: value); }
        }
    }
}
