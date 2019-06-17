using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public enum AppSettings
    {
        ShowSongNotifications,
        TryToFindSongMetadata,

        MediaBarMatchStationColor,
        NavigateToStationWhenLaunched,

        PreferUsingCrossFadeWhenChangingStations,

        UseHapticFeedbackForNavigation,

        SelectedBluetoothDevice,
        SelectedBluetoothDeviceName,
        SaySongNotificationsInBluetoothMode,
        UpdateLockScreenWithSongArt,
        FallBackLockScreenImageUri,

        AutomaticallyConserveDataWhenOnMeteredConnections,
        AutomaticallyDetermineAppropriateBitrateBasedOnConnection,
    }
}
