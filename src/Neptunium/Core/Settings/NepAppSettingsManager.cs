using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using static Neptunium.NepApp;

namespace Neptunium.Core.Settings
{
    public class NepAppSettingsManager: INepAppFunctionManager
    {
        internal NepAppSettingsManager()
        {
            //initialize app settings
            //todo add all settings

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.ShowSongNotifications)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.ShowSongNotifications), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.TryToFindSongMetadata)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.TryToFindSongMetadata), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.NavigateToStationWhenLaunched)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.NavigateToStationWhenLaunched), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.MediaBarMatchStationColor)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.MediaBarMatchStationColor), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.PreferUsingCrossFadeWhenChangingStations)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.PreferUsingCrossFadeWhenChangingStations), true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Enum.GetName(typeof(AppSettings), AppSettings.UseHapticFeedbackForNavigation)))
                ApplicationData.Current.LocalSettings.Values.Add(Enum.GetName(typeof(AppSettings), AppSettings.UseHapticFeedbackForNavigation), true);
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
        }

        public void SetSetting(AppSettings setting, object value)
        {
            SetSetting(Enum.GetName(typeof(AppSettings), setting), value);
        }
    }
}
