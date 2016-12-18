using Crystal3.Model;
using Neptunium.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Managers.Songs;

namespace Neptunium.ViewModel
{
    public class SongHistoryViewModel : UIViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            await UI.WaitForUILoadAsync();

            IsBusy = true;

            var songs = SongManager.HistoryManager.SongHistory.ToArray();
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                SongHistory = new ObservableCollection<SongHistoryItem>(songs);
            });

            SongManager.HistoryManager.ItemAdded += SongHistoryManager_ItemAdded;
            SongManager.HistoryManager.ItemRemoved += SongHistoryManager_ItemRemoved;

            IsBusy = false;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            SongManager.HistoryManager.ItemAdded -= SongHistoryManager_ItemAdded;
            SongManager.HistoryManager.ItemRemoved -= SongHistoryManager_ItemRemoved;
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
