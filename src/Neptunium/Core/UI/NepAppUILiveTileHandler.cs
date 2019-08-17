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
            NepApp.SongManager.ArtworkProcessor.SongArtworkAvailable += ArtworkProcessor_SongArtworkAvailable;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;

            if (!NepApp.MediaPlayer.IsPlaying) nepAppUIManager?.ClearLiveTileAndMediaNotifcation();
        }

        private void ArtworkProcessor_SongArtworkAvailable(object sender, NepAppSongMetadataArtworkEventArgs e)
        {
            if (e.CurrentMetadata != null)
                NepApp.UI.Notifier.UpdateExtendedMetadataLiveTile((ExtendedSongMetadata)e.CurrentMetadata);
        }

        private void SongManager_StationRadioProgramStarted(object sender, NepAppStationProgramStartedEventArgs e)
        {
            if (e.Metadata != null)
                NepApp.UI.Notifier.UpdateLiveTile(new ExtendedSongMetadata(e.Metadata));
        }

        private void SongManager_SongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            //if (e.Metadata != null)
            //    NepApp.UI.Notifier.UpdateExtendedMetadataLiveTile((ExtendedSongMetadata)e.Metadata);
        }

        private void SongManager_PreSongChanged(object sender, NepAppSongChangedEventArgs e)
        {
            if (e.Metadata != null)
                NepApp.UI.Notifier.UpdateLiveTile(e.Metadata);
        }
    }
}
