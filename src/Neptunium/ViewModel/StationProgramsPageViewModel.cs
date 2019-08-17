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
            if (ScheduleItems == null)
            {
                IsBusy = true;

                try
                {
                    //await LoadScheduleAsync();
                }
                catch
                {

                }

                IsBusy = false;
            }

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            if (SortedScheduleItems != null)
                SortedScheduleItems.Source = null;

            ScheduleItems?.Clear();
            ScheduleItems = null;

            base.OnNavigatedFrom(sender, e);
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
                        item.Station = await NepApp.Stations.GetStationByNameAsync(program.Station);
                        item.Day = Enum.GetName(typeof(DayOfWeek), listing.Day);
                        item.TimeLocal = listing.Time;
                        item.Program = program;

                        items.Add(item);
                    }
                }
            }

            ScheduleItems = new ObservableCollection<ScheduleItem>(items);
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = items
                .OrderBy(x => x.TimeLocal.Hour)
                .GroupBy(x => x.Day)
                .OrderBy(x => x.Key, new DayComparer());
            collectionViewSource.IsSourceGrouped = true;
            SortedScheduleItems = collectionViewSource;

            RaisePropertyChanged(nameof(ScheduleItems));
            RaisePropertyChanged(nameof(SortedScheduleItems));
        }

        private class DayComparer : IComparer<string>
        {
            private int GetDayNumber(string day)
            {
                switch (day.ToLower())
                {
                    case "sunday": return 0;
                    case "monday": return 1;
                    case "tuesday": return 2;
                    case "wednesday": return 3;
                    case "thursday": return 4;
                    case "friday": return 5;
                    case "saturday": return 6;
                }

                return 0;
            }
            public int Compare(string x, string y)
            {
                if (GetDayNumber(x) > GetDayNumber(y)) return 1;
                else if (GetDayNumber(x) < GetDayNumber(y)) return -1;
                else return 0;
            }
        }
    }
}
