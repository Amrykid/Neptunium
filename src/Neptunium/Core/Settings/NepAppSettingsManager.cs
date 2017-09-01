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
            List<KeyValuePair<string, object>> settings = new List<KeyValuePair<string, object>>();
            foreach (string info in Enum.GetNames(typeof(AppSettings)))
            {
                settings.Add(new KeyValuePair<string, object>(info, ApplicationData.Current.LocalSettings.Values[info]));
            }

            return settings;
        }
    }
}
