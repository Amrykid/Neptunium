using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3;

namespace Neptunium.ViewModel
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public RelayCommand ClearBluetoothDeviceCommand => new RelayCommand(x =>
        {
            if (NepApp.Media.Bluetooth.DeviceCoordinator.IsInitialized)
            {
                NepApp.Media.Bluetooth.DeviceCoordinator.ClearDevice();

                SelectedBluetoothDeviceName = "None";
            }
        });

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            if (CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Xbox)
            {
                if (await NepApp.Media.Bluetooth.DeviceCoordinator.HasBluetoothRadiosAsync())
                {
                    SelectedBluetoothDeviceName = NepApp.Media.Bluetooth.DeviceCoordinator.SelectedBluetoothDeviceName ?? "None";
                }
            }

            base.OnNavigatedTo(sender, e);
        }

        public bool ShowSongNotification
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications); }
            set { NepApp.Settings.SetSetting(AppSettings.ShowSongNotifications, value); }
        }

        public bool FindSongMetadata
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata); }
            set { NepApp.Settings.SetSetting(AppSettings.TryToFindSongMetadata, value); }
        }

        public bool SaySongNotifications
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsInBluetoothMode); }
            set { NepApp.Settings.SetSetting(AppSettings.SaySongNotificationsInBluetoothMode, value); }
        }

        public bool UpdateLockScreenWithSongArt
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt); }
            set { NepApp.Settings.SetSetting(AppSettings.UpdateLockScreenWithSongArt, value); }
        }

        public string SelectedBluetoothDeviceName
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value: value); }
        }
    }
}
