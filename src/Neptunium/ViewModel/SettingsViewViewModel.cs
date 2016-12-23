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
                    var selection = await CarModeManager.SelectDeviceAsync(Windows.UI.Xaml.Window.Current.Bounds);

                    if (selection != null)
                    {
                        SelectedBluetoothDevice = selection.Name;

                        ClearCarModeDeviceCommand.SetCanExecute(selection != null);
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

        public bool ShouldFetchSongMetadata
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        public bool ShouldHaveMediaBarMatchStationColor
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        public bool ShouldNavigateToStationPageWhenLaunching
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }

        public bool ShouldPreferCrossFadingOnStationTransition
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
            set
            {
                SetPropertyValue<bool>(value: value);

                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
                    CarModeManager.SetShouldAnnounceSongs(value);
                }
            }
        }

        public string SelectedBluetoothDevice
        {
            get { return GetPropertyValue<string>(); }
            private set { SetPropertyValue<string>(value: value); }
        }

        public bool JapaneseVoiceForSongAnnouncements
        {
            get { return GetPropertyValue<bool>(); }
            set
            {
                SetPropertyValue<bool>(value: value);
                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
                    CarModeManager.SetShouldUseJapaneseVoice(value);
                }
            }
        }
        #endregion

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            ShouldStopPlayingAfterSuccessfulHandoff = ContinuedAppExperienceManager.StopPlayingStationOnThisDeviceAfterSuccessfulHandoff;

            ShouldShowSongNofitications = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications];

            ShouldFetchSongMetadata = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.TryToFindSongMetadata];

            ShouldHaveMediaBarMatchStationColor = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.MediaBarMatchStationColor];

            ShouldNavigateToStationPageWhenLaunching = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.NavigateToStationWhenLaunched];

            ShouldPreferCrossFadingOnStationTransition = (bool)ApplicationData.Current.LocalSettings.Values[AppSettings.PreferUsingCrossFadeWhenChangingStations];

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                CarModeAnnounceSongs = CarModeManager.ShouldAnnounceSongs;
                SelectedBluetoothDevice = CarModeManager.SelectedDevice?.Name ?? "None";
                ClearCarModeDeviceCommand.SetCanExecute(CarModeManager.SelectedDevice != null);
                JapaneseVoiceForSongAnnouncements = CarModeManager.ShouldUseJapaneseVoice;
            }
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            ContinuedAppExperienceManager.SetStopPlayingStationOnThisDeviceAfterSuccessfulHandoff(ShouldStopPlayingAfterSuccessfulHandoff);

            ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] = ShouldShowSongNofitications;

            ApplicationData.Current.LocalSettings.Values[AppSettings.TryToFindSongMetadata] = ShouldFetchSongMetadata;

            ApplicationData.Current.LocalSettings.Values[AppSettings.MediaBarMatchStationColor] = ShouldHaveMediaBarMatchStationColor;

            ApplicationData.Current.LocalSettings.Values[AppSettings.NavigateToStationWhenLaunched] = ShouldNavigateToStationPageWhenLaunching;

            ApplicationData.Current.LocalSettings.Values[AppSettings.PreferUsingCrossFadeWhenChangingStations] = ShouldPreferCrossFadingOnStationTransition;

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                CarModeManager.SetShouldAnnounceSongs(CarModeAnnounceSongs);
                CarModeManager.SetShouldUseJapaneseVoice(JapaneseVoiceForSongAnnouncements);
            }
        }
    }
}
