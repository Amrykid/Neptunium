using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Core.Media.Metadata;
using Neptunium.Media.Songs;
using Windows.UI.Core;
using Windows.UI.Notifications;

namespace Neptunium.Core.UI
{
    public class NepAppUILiveTileHandler
    {
        internal NepAppUILiveTileHandler()
        {
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;

            if (!NepApp.MediaPlayer.IsPlaying) ClearLiveTileAndMediaNotifcation();
        }

        internal void ClearLiveTileAndMediaNotifcation()
        {
            if (!NepApp.IsServerMode)
            {
                if (App.GetDevicePlatform() == Crystal3.Core.Platform.Desktop || App.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
                    //clears the tile if we're suspending.
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                }

                if (!NepApp.MediaPlayer.IsPlaying)
                {
                    //removes the now playing notification from the action center.
                    ToastNotificationManager.History.Remove(NepAppUIManagerNotifier.SongNotificationTag);
                }
            }
        }

        private void SongManager_StationRadioProgramStarted(object sender, NepAppStationProgramStartedEventArgs e)
        {
            if (e.Metadata != null)
                NepApp.UI.Notifier.UpdateLiveTile(new ExtendedSongMetadata(e.Metadata));
        }

        private void SongManager_SongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            NepApp.UI.Notifier.UpdateLiveTile((ExtendedSongMetadata)e.Metadata);
        }

        private void SongManager_PreSongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            
        }
    }
}
