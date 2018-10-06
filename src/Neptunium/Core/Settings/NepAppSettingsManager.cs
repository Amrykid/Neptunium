using Crystal3;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using static Neptunium.NepApp;

namespace Neptunium.Core.Settings
{
    public class NepAppSettingsManager : INepAppFunctionManager
    {
        public event EventHandler<NepAppSettingChangedEventArgs> SettingChanged;

        internal NepAppSettingsManager()
        {
            //initialize app settings
            //todo add all settings

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.ShowSongNotifications)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.ShowSongNotifications), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.TryToFindSongMetadata)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.TryToFindSongMetadata), true); //todo make this app setting false by default when we hit v1.0
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.NavigateToStationWhenLaunched)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.NavigateToStationWhenLaunched), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.MediaBarMatchStationColor)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.MediaBarMatchStationColor), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.PreferUsingCrossFadeWhenChangingStations)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.PreferUsingCrossFadeWhenChangingStations), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.UseHapticFeedbackForNavigation)))
            {
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.UseHapticFeedbackForNavigation), CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile);
            }
            else
            {
                //ensure that for Xbox, this is always false.

                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                {
                    SetSetting(AppSettings.UseHapticFeedbackForNavigation, false);
                }
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.UpdateLockScreenWithSongArt)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.UpdateLockScreenWithSongArt), false);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.AutomaticallyConserveDataWhenOnMeteredConnections)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.AutomaticallyConserveDataWhenOnMeteredConnections), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.AutomaticallyDetermineAppropriateBitrateBasedOnConnection)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.AutomaticallyDetermineAppropriateBitrateBasedOnConnection), true);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.ShowRemoteMenu)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.ShowRemoteMenu), false);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.SaySongNotificationsWhenHeadphonesAreConnected)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.SaySongNotificationsWhenHeadphonesAreConnected), false);


            //bluetooth mode stuff
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.SaySongNotificationsInBluetoothMode)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.SaySongNotificationsInBluetoothMode), false);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.SelectedBluetoothDevice)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.SelectedBluetoothDevice), null);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.SelectedBluetoothDeviceName)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.SelectedBluetoothDeviceName), "");
        }

        public IEnumerable<KeyValuePair<string, object>> GetAllSettings()
        {
            //yield seems to prevent the call on this method from working. doing it the old and terrible way.

            List<KeyValuePair<string, object>> settings = new List<KeyValuePair<string, object>>();

            foreach (string settingName in Enum.GetNames(typeof(AppSettings)))
            {
                settings.Add(new KeyValuePair<string, object>(settingName, ApplicationData.Current.LocalSettings.Values[settingName]));
            }

            return settings;
        }

        public object GetSetting(string settingName)
        {
            if (string.IsNullOrWhiteSpace(settingName)) throw new ArgumentNullException(nameof(settingName));
            if (!Enum.GetNames(typeof(AppSettings)).Contains(settingName))
                throw new ArgumentOutOfRangeException(paramName: nameof(settingName), message: "Setting not found.");


            return ApplicationData.Current.LocalSettings.Values[settingName];
        }

        public object GetSetting(AppSettings setting)
        {
            return GetSetting(Enum.GetName(typeof(AppSettings), setting));
        }

        public void SetSetting(string settingName, object value)
        {
            if (string.IsNullOrWhiteSpace(settingName)) throw new ArgumentNullException(nameof(settingName));
            if (!Enum.GetNames(typeof(AppSettings)).Contains(settingName))
                throw new ArgumentOutOfRangeException(paramName: nameof(settingName), message: "Setting not found.");

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(settingName))
                ApplicationData.Current.LocalSettings.Values.Add(settingName, value);
            else
                ApplicationData.Current.LocalSettings.Values[settingName] = value;

            SettingChanged?.Invoke(this, new NepAppSettingChangedEventArgs()
            {
                ChangedSetting = (AppSettings)Enum.Parse(typeof(AppSettings), settingName),
                NewValue = value
            });
        }

        public void SetSetting(AppSettings setting, object value)
        {
            SetSetting(Enum.GetName(typeof(AppSettings), setting), value);
        }

        public bool ContainsSetting(AppSettings setting)
        {
            return ContainsSetting(Enum.GetName(typeof(AppSettings), setting));
        }

        public bool ContainsSetting(string settingName)
        {
            if (string.IsNullOrWhiteSpace(settingName)) throw new ArgumentNullException(nameof(settingName));
            if (!Enum.GetNames(typeof(AppSettings)).Contains(settingName))
                throw new ArgumentOutOfRangeException(paramName: nameof(settingName), message: "Setting not found.");

            return ApplicationData.Current.LocalSettings.Values.ContainsKey(settingName);
        }
    }
}
