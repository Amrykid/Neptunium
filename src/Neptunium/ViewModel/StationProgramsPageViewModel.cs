using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using System.Collections.ObjectModel;
using Neptunium.Model;
using Windows.UI.Xaml.Data;

namespace Neptunium.ViewModel
{
    public class StationProgramsPageViewModel : UIViewModelBase
    {
        public CollectionViewSource SortedScheduleItems
        {
            get { return GetPropertyValue<CollectionViewSource>(); }
            private set { SetPropertyValue<CollectionViewSource>(value: value); }
        }

        public ObservableCollection<ScheduleItem> ScheduleItems
        {
            get { return GetPropertyValue<ObservableCollection<ScheduleItem>>(); }
            private set { SetPropertyValue<ObservableCollection<ScheduleItem>>(value: value); }
        }


        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (e.Direction == CrystalNavigationDirection.Forward)
            {
                IsBusy = true;

                try
                {
                    await LoadScheduleAsync();
                }
                catch
                {

                }

                IsBusy = false;
            }

            base.OnNavigatedTo(sender, e);
        }

        private async Task LoadScheduleAsync()
        {
            var items = new List<ScheduleItem>();

            var stations = await NepApp.Stations.GetStationsAsync();

            var stationsWithPrograms = stations.Where(x => x.Programs != null);
            var allPrograms = stationsWithPrograms.SelectMany(x => x.Programs).ToArray();

            foreach (var program in allPrograms)
            {
                if (program.TimeListings != null)
                {
                    foreach (var listing in program.TimeListings)
                    {
                        ScheduleItem item = new ScheduleItem();
                        item.Station = program.Station;
                        item.Day = Enum.GetName(typeof(DayOfWeek), listing.Day);
                        item.TimeLocal = listing.Time;
                        item.Program = program;

                        items.Add(item);
                    }
                }
            }

            ScheduleItems = new ObservableCollection<ScheduleItem>(items);
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = items. OrderBy(x => x.TimeLocal.Hour).GroupBy(x => x.Day);
            collectionViewSource.IsSourceGrouped = true;
            SortedScheduleItems = collectionViewSource;

            RaisePropertyChanged(nameof(ScheduleItems));
            RaisePropertyChanged(nameof(SortedScheduleItems));
        }
    }
}
