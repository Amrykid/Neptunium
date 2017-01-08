using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public static class AppSettings
    {
        public const string ShowSongNotifications = nameof(ShowSongNotifications);
        public const string TryToFindSongMetadata = nameof(TryToFindSongMetadata);

        public const string MediaBarMatchStationColor = nameof(MediaBarMatchStationColor);
        public const string NavigateToStationWhenLaunched = nameof(NavigateToStationWhenLaunched);

        public const string PreferUsingCrossFadeWhenChangingStations = nameof(PreferUsingCrossFadeWhenChangingStations);

        public const string UseHapticFeedbackForNavigation = nameof(UseHapticFeedbackForNavigation);
    }
}
