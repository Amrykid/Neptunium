using Crystal3.Model;
using Crystal3.UI.Commands;
using Neptunium.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Windows.Storage;

namespace Neptunium.ViewModel
{
    public class SettingsViewViewModel: ViewModelBase
    {
        public SettingsViewViewModel()
        {
            PickDeviceCommand = new RelayCommand(async x =>
            {
                await CarModeManager.SelectDeviceAsync(Windows.UI.Xaml.Window.Current.Bounds);
                SelectedBluetoothDevice = CarModeManager.SelectedDevice.Name;

                //todo add a way to clear the selected bluetooth device for car mode.
            });
        }

        public RelayCommand PickDeviceCommand { get; private set; }

        public bool ShouldShowSongNofitications
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        #region Car Mode Settings
        public bool CarModeAnnounceSongs
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        public string SelectedBluetoothDevice
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }
        #endregion

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            CarModeAnnounceSongs = CarModeManager.ShouldAnnounceSongs;

            SelectedBluetoothDevice = CarModeManager.SelectedDevice?.Name;

            ShouldShowSongNofitications = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications];
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            CarModeManager.SetShouldAnnounceSongs(CarModeAnnounceSongs);

            ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] = ShouldShowSongNofitications;
        }
    }
}
