using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Core.Media.Metadata;
using Neptunium.Media.Songs;

namespace Neptunium.Core.UI
{
    public class NepAppUILiveTileHandler
    {
        internal NepAppUILiveTileHandler()
        {
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;
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
