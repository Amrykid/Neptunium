using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using static Neptunium.NepApp;

namespace Neptunium.Core
{
    public class NepAppSettingsManager: INepAppFunctionManager
    {
        internal NepAppSettingsManager()
        {
            //initialize app settings
            //todo add all settings

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.ShowSongNotifications))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.ShowSongNotifications, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.TryToFindSongMetadata))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.TryToFindSongMetadata, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.NavigateToStationWhenLaunched))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.NavigateToStationWhenLaunched, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.MediaBarMatchStationColor))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.MediaBarMatchStationColor, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.PreferUsingCrossFadeWhenChangingStations))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.PreferUsingCrossFadeWhenChangingStations, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.UseHapticFeedbackForNavigation))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.UseHapticFeedbackForNavigation, true);
        }
    }
}
