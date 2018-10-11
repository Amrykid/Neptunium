using Neptunium.Media.Songs;
using Neptunium.Core.Media.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.UI
{
    public class NepAppUIToastNotificationHandler
    {
        internal NepAppUIToastNotificationHandler()
        {
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;
            NepApp.MediaPlayer.FatalMediaErrorOccurred += MediaPlayer_FatalMediaErrorOccurred;
        }

        private async void SongManager_StationRadioProgramStarted(object sender, 
            NepAppStationProgramStartedEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
            {
                if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
                {
                    if (e.RadioProgram.Style == Core.Stations.StationProgramStyle.Block)
                    {
                        NepApp.UI.Notifier.ShowStationBlockProgrammingToastNotification(
                            e.RadioProgram, e.Metadata);
                    }
                    else
                    {
                        NepApp.UI.Notifier.ShowStationHostedProgrammingToastNotification(
                            e.RadioProgram, e.Metadata);
                    }
                }
                else
                {
                    if (e.RadioProgram.Style == Core.Stations.StationProgramStyle.Block)
                    {
                        await NepApp.UI.Overlay.ShowSnackBarMessageAsync(
                            "Tuning into " + e.RadioProgram.Name + " on " + e.Station);
                    }
                    else
                    {
                        await NepApp.UI.Overlay.ShowSnackBarMessageAsync(
                            "Tuning into " + e.RadioProgram.Name + " by " + e.RadioProgram.Host);
                    }
                }
            }
        }

        private async void MediaPlayer_FatalMediaErrorOccurred(object sender, 
            Windows.Media.Playback.MediaPlayerFailedEventArgs e)
        {
            if (!await App.GetIfPrimaryWindowVisibleAsync())
            {
                NepApp.UI.Notifier.ShowErrorToastNotification(null, "Uh-Oh!", 
                    !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");
            }
        }

        private async void SongManager_SongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
            {
                if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
                    NepApp.UI.Notifier.ShowSongToastNotification((ExtendedSongMetadata)e.Metadata);
            }
        }

        private void SongManager_PreSongChanged(object sender, NepAppSongChangedEventArgs e)
        {

        }
    }
}
