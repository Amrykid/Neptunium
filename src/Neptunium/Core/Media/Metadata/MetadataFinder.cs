using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Neptunium.Core.Media.Metadata
{
    public static class MetadataFinder
    {
        public static StorageFile BuiltInArtistsFile = null;
        public static async Task<ExtendedSongMetadata> FindMetadataAsync(SongMetadata originalMetadata)
        {
            var metaSrc = new MusicBrainzMetadataSource();
            AlbumData albumData = null;
            ArtistData artistData = null;
            var station = await NepApp.Stations.GetStationByNameAsync(originalMetadata.StationPlayedOn);
            var extendedMetadata = new ExtendedSongMetadata(originalMetadata);

            //todo strip out "feat." artists

            if (Windows.System.Power.PowerManager.EnergySaverStatus != Windows.System.Power.EnergySaverStatus.On)
            {
                if (NepApp.Network.NetworkUtilizationBehavior == NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.Normal)
                {
                    if ((bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata))
                    {
                        try
                        {
                            albumData = await metaSrc.TryFindAlbumAsync(originalMetadata.Track, originalMetadata.Artist, station.PrimaryLocale);

                            await Task.Delay(250); //250 ms sleep

                            artistData = await metaSrc.TryFindArtistAsync(originalMetadata.Artist, station.PrimaryLocale);

                            await Task.Delay(250); //250 ms sleep

                            //await metaSrc.TryFindSongAsync(extendedMetadata, station.PrimaryLocale);

                            extendedMetadata.JPopAsiaArtistInfo = await ArtistFetcher.FindArtistDataOnJPopAsiaAsync(originalMetadata.Artist.Trim());

                            extendedMetadata.FanArtTVBackgroundUrl = await FanArtTVFetcher.FetchArtistBackgroundAsync(originalMetadata.Artist.Trim());
                        }
                        catch (Exception) { }
                    }
                }
            }

            //todo cache
            extendedMetadata.Album = albumData;
            extendedMetadata.ArtistInfo = artistData;

            return extendedMetadata;
        }
    }
}
