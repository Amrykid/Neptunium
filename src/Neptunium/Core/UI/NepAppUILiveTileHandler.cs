using Neptunium.Core.Media.Metadata;
using Neptunium.Media.Songs;

namespace Neptunium.Core.UI
{
    public class NepAppUILiveTileHandler
    {
        internal NepAppUILiveTileHandler(NepAppUIManager nepAppUIManager)
        {
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;

            if (!NepApp.MediaPlayer.IsPlaying) nepAppUIManager?.ClearLiveTileAndMediaNotifcation();
        }

        private void SongManager_StationRadioProgramStarted(object sender, NepAppStationProgramStartedEventArgs e)
        {
            if (e.Metadata != null)
                NepApp.UI.Notifier.UpdateLiveTile(new ExtendedSongMetadata(e.Metadata));
        }

        private void SongManager_SongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            NepApp.UI.Notifier.UpdateExtendedMetadataLiveTile((ExtendedSongMetadata)e.Metadata);
        }

        private void SongManager_PreSongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            NepApp.UI.Notifier.UpdateLiveTile(e.Metadata);
        }
    }
}
