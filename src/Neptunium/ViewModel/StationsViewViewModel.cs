using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3.UI;
using Crystal3.Utilities;
using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace Neptunium.ViewModel
{
    public class StationsViewViewModel : UIViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            await UI.WaitForUILoadAsync();

            IsBusy = true;

            if (e.Direction == CrystalNavigationDirection.Forward || e.Direction == CrystalNavigationDirection.Restore || e.Direction == CrystalNavigationDirection.Refresh)
            {
                if (Stations == null)
                {
                    await App.Dispatcher.RunWhenIdleAsync(() =>
                    {
                        Stations = new ObservableCollection<StationModel>(StationDataManager.Stations);
                    });
                }

            }

            IsBusy = false;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            base.OnNavigatedFrom(sender, e);
        }

        public ObservableCollection<StationModel> Stations
        {
            get { return GetPropertyValue<ObservableCollection<StationModel>>("Stations"); }
            private set { SetPropertyValue<ObservableCollection<StationModel>>("Stations", value); }
        }
    }
}
