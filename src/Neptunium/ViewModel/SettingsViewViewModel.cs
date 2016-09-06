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
using Crystal3;

namespace Neptunium.ViewModel
{
    public class SettingsViewViewModel : ViewModelBase
    {
        public SettingsViewViewModel()
        {
            PickCarModeDeviceCommand = new RelayCommand(async x =>
            {
#if RELEASE
                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
#endif
                try
                {
                    await CarModeManager.SelectDeviceAsync(Windows.UI.Xaml.Window.Current.Bounds);

                    if (CarModeManager.SelectedDevice != null)
                    {
                        SelectedBluetoothDevice = CarModeManager.SelectedDevice?.Name;

                        ClearCarModeDeviceCommand.SetCanExecute(CarModeManager.SelectedDevice != null);
                    }
                    else { SelectedBluetoothDevice = "None"; }
                }
                catch (Exception) { }
#if RELEASE
                }
#endif
            });

            ClearCarModeDeviceCommand = new ManualRelayCommand(x =>
            {
                CarModeManager.ClearDevice();

                SelectedBluetoothDevice = "None";
            });
        }

        public bool ShouldShowSongNofitications
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        public bool ShouldStopPlayingAfterSuccessfulHandoff
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        #region Car Mode Settings

        public RelayCommand PickCarModeDeviceCommand { get; private set; }
        public ManualRelayCommand ClearCarModeDeviceCommand { get; private set; }

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

        public bool JapaneseVoiceForSongAnnouncements
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }
#endregion

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
#if RELEASE
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
#endif
                CarModeAnnounceSongs = CarModeManager.ShouldAnnounceSongs;
                SelectedBluetoothDevice = CarModeManager.SelectedDevice?.Name ?? "None";
                ClearCarModeDeviceCommand.SetCanExecute(CarModeManager.SelectedDevice != null);
                JapaneseVoiceForSongAnnouncements = CarModeManager.ShouldUseJapaneseVoice;
#if RELEASE
            }
#endif

            ShouldStopPlayingAfterSuccessfulHandoff = ContinuedAppExperienceManager.StopPlayingStationOnThisDeviceAfterSuccessfulHandoff;

            ShouldShowSongNofitications = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications];
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
#if RELEASE
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
#endif
                CarModeManager.SetShouldAnnounceSongs(CarModeAnnounceSongs);
                CarModeManager.SetShouldUseJapaneseVoice(JapaneseVoiceForSongAnnouncements);
#if RELEASE
            }
#endif

            ContinuedAppExperienceManager.SetStopPlayingStationOnThisDeviceAfterSuccessfulHandoff(ShouldStopPlayingAfterSuccessfulHandoff);

            ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] = ShouldShowSongNofitications;
        }
    }
}
