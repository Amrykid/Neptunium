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
            if (NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.IsInitialized)
            {
                NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.ClearDevice();

                SelectedBluetoothDeviceName = "None";
            }
        });

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Settings.SettingChanged += Settings_SettingChanged;

            if (DeviceInformation.GetDevicePlatform() != Crystal3.Core.Platform.Xbox)
            {
                if (await NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.HasBluetoothRadiosAsync())
                {
                    SelectedBluetoothDeviceName = NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.SelectedBluetoothDeviceName ?? "None";
                }
            }

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Settings.SettingChanged -= Settings_SettingChanged;

            base.OnNavigatedFrom(sender, e);
        }

        private void Settings_SettingChanged(object sender, Core.Settings.NepAppSettingChangedEventArgs e)
        {
            switch (e.ChangedSetting)
            {
                case AppSettings.FallBackLockScreenImageUri:
                    RaisePropertyChanged(nameof(FallBackLockScreenArtworkUri));
                    break;
            }
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

        public bool SaySongNotificationsInBluetoothMode
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsInBluetoothMode); }
            set { NepApp.Settings.SetSetting(AppSettings.SaySongNotificationsInBluetoothMode, value); }
        }

        public bool SaySongNotificationsWhenHeadphonesConnected
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsWhenHeadphonesAreConnected); }
            set { NepApp.Settings.SetSetting(AppSettings.SaySongNotificationsWhenHeadphonesAreConnected, value); }
        }

        public bool UpdateLockScreenWithSongArt
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt); }
            set { NepApp.Settings.SetSetting(AppSettings.UpdateLockScreenWithSongArt, value); }
        }

        public string FallBackLockScreenArtwork
        {
            get { return (string)NepApp.Settings.GetSetting(AppSettings.FallBackLockScreenImageUri); }
            set
            {
                NepApp.Settings.SetSetting(AppSettings.FallBackLockScreenImageUri, value);
                RaisePropertyChanged(nameof(FallBackLockScreenArtworkUri));
            }
        }

        public Uri FallBackLockScreenArtworkUri
        {
            get
            {
                return NepApp.UI.LockScreen.FallbackLockScreenImage;
            }
        }

        public string SelectedBluetoothDeviceName
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value: value); }
        }

        public bool ConserveData
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.AutomaticallyConserveDataWhenOnMeteredConnections); }
            set
            {
                NepApp.Settings.SetSetting(AppSettings.AutomaticallyConserveDataWhenOnMeteredConnections, value);

                if (!value)
                {
                    //automatically set Choose Bitrate to false if "Conserve Data" is turned off.
                    ChooseBitrate = false;
                    RaisePropertyChanged(nameof(ChooseBitrate));
                }
            }
        }

        public bool ChooseBitrate
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.AutomaticallyDetermineAppropriateBitrateBasedOnConnection); }
            set { NepApp.Settings.SetSetting(AppSettings.AutomaticallyDetermineAppropriateBitrateBasedOnConnection, value); }
        }

        public bool UseHapticFeedback
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.UseHapticFeedbackForNavigation); }
            set { NepApp.Settings.SetSetting(AppSettings.UseHapticFeedbackForNavigation, value); }
        }
    }
}
