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
            NepApp.SongManager.History.SongAdded += History_SongAdded;
            UpdateHistory(NepApp.SongManager.History.HistoryOfSongs);

            base.OnNavigatedTo(sender, e);
        }

        private void History_SongAdded(object sender, SongHistorianSongUpdatedEventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                UpdateHistory(NepApp.SongManager.History.HistoryOfSongs as List<SongHistoryItem>);
            });
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            History = null;

            base.OnNavigatedFrom(sender, e);
        }


        private void UpdateHistory(List<SongHistoryItem> collection)
        {
            History = collection.GroupBy(x => x.PlayedDate.Date).OrderByDescending(x => x.Key).Select(x => x);
        }

        public IEnumerable<IGrouping<DateTime, SongHistoryItem>> History
        {
            get { return GetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(); }
            private set { SetPropertyValue<IEnumerable<IGrouping<DateTime, SongHistoryItem>>>(value: value); }
        }

        public RelayCommand CopyMetadataCommand => new RelayCommand(x =>
        {
            if (x == null) return;
            if (x is SongHistoryItem)
            {
                SongHistoryItem item = (SongHistoryItem)x;

                DataPackage package = new DataPackage();
                package.Properties.Description = "Song Metadata";
                package.Properties.Title = item.Metadata.Track;
                package.Properties.ApplicationName = "Neptunium";
                package.SetText(item.Metadata.ToString());
                Clipboard.SetContent(package);

                NepApp.UI.Overlay.ShowSnackBarMessageAsync("Copied");
            }
        });
    }
}
