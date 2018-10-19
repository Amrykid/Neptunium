using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core.Media.History;
using System.Collections.ObjectModel;
using Crystal3.UI.Commands;
using Windows.ApplicationModel.DataTransfer;

namespace Neptunium.ViewModel
{
    public class SongHistoryPageViewModel : ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            History = new ObservableCollection<SongHistoryItem>();

            NepApp.SongManager.History.SongAdded += History_SongAdded;

            NepApp.SongManager.History.GetHistoryOfSongsAsync().Subscribe(item =>
            {
                History.Add(item);
            });

            base.OnNavigatedTo(sender, e);
        }

        private void History_SongAdded(object sender, SongHistorianSongUpdatedEventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                History.Insert(0, e.Item);
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.SongManager.History.SongAdded -= History_SongAdded;
            History = null;

            base.OnNavigatedFrom(sender, e);
        }
        

        public ObservableCollection<SongHistoryItem> History
        {
            get { return GetPropertyValue<ObservableCollection<SongHistoryItem>>(); }
            private set { SetPropertyValue<ObservableCollection<SongHistoryItem>>(value: value); }
        }

        public RelayCommand CopyMetadataCommand => new RelayCommand(x =>
        {
            if (x == null) return;
            if (x is SongHistoryItem)
            {
                SongHistoryItem item = (SongHistoryItem)x;

                DataPackage package = new DataPackage();
                package.Properties.Description = "Song Metadata";
                package.Properties.Title = item.Track;
                package.Properties.ApplicationName = "Neptunium";
                package.SetText(item.ToString());
                Clipboard.SetContent(package);

                NepApp.UI.Overlay.ShowSnackBarMessageAsync("Copied");
            }
        });
    }
}
