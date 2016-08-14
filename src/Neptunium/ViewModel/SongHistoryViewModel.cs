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
    public class SongHistoryViewModel : ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            try
            {
                SongHistory = new ObservableCollection<SongHistoryItem>(SongHistoryManager.SongHistory);
            }
            catch (Exception)
            {
                SongHistory = new ObservableCollection<SongHistoryItem>();
            }

            SongHistoryManager.ItemAdded += SongHistoryManager_ItemAdded;
            SongHistoryManager.ItemRemoved += SongHistoryManager_ItemRemoved;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            SongHistoryManager.ItemAdded -= SongHistoryManager_ItemAdded;
            SongHistoryManager.ItemRemoved -= SongHistoryManager_ItemRemoved;
        }

        private void SongHistoryManager_ItemRemoved(object sender, SongHistoryManagerItemRemovedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                if (SongHistory.Contains(e.RemovedItem))
                    SongHistory.Remove(e.RemovedItem);
            });
        }

        private void SongHistoryManager_ItemAdded(object sender, SongHistoryManagerItemAddedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                SongHistory.Insert(0, e.AddedItem);
            });
        }

        public ObservableCollection<SongHistoryItem> SongHistory
        {
            get { return GetPropertyValue<ObservableCollection<SongHistoryItem>>(); }
            set { SetPropertyValue<ObservableCollection<SongHistoryItem>>(value: value); }
        }
    }
}
