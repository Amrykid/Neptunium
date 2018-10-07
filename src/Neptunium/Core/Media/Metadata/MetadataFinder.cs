using Neptunium.Model;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Neptunium.Core.Media.Metadata
{
    /// <summary>
    /// A class for finding extended metadata on a particular song (track and artist).
    /// </summary>
    public static class MetadataFinder
    {
        public static StorageFile BuiltInArtistsFile = null;

        /// <summary>
        /// Finds extended metadata on a particular song (track and artist).
        /// </summary>
        /// <param name="originalMetadata">The song to find more metadata on.</param>
        /// <returns>ExtendedMetadata on the song requested.</returns>
        public static async Task<ExtendedSongMetadata> FindMetadataAsync(SongMetadata originalMetadata)
        {
            //Checks if original metadata is null.
            if (originalMetadata == null) throw new ArgumentNullException(nameof(originalMetadata));
            //Checks if Unknown Metadata was passed ("Unknown Song - Unknown Artist").
            if (originalMetadata.IsUnknownMetadata) throw new ArgumentException("Unknown metadata was passed.", nameof(originalMetadata));

            //Defines our musicbrainz metadata source and place holder objects.
            var metaSrc = new MusicBrainzMetadataSource();
            AlbumData albumData = null;
            ArtistData artistData = null;
            var station = await NepApp.Stations.GetStationByNameAsync(originalMetadata.StationPlayedOn);
            var extendedMetadata = new ExtendedSongMetadata(originalMetadata);

            //todo strip out "feat." artists

            //Checks if we're on battery saver mode.
            if (Windows.System.Power.PowerManager.EnergySaverStatus != Windows.System.Power.EnergySaverStatus.On)
            {
                //We're not on battery saver mode. Let's check to see if we're on a metered network or roaming.
                if (NepApp.Network.NetworkUtilizationBehavior == NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.Normal)
                {
                    //We're not metered or roaming. Lastly, lets check if the user wants us to find song metadata.
                    if ((bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata))
                    {
                        //First, grab album data from musicbrainz.
                        albumData = await metaSrc.TryFindAlbumAsync(originalMetadata.Track, originalMetadata.Artist, station.PrimaryLocale);

                        await Task.Delay(500); //500 ms sleep

                        //Next, try and grab artist data from musicbrainz.
                        artistData = await metaSrc.TryFindArtistAsync(originalMetadata.Artist, station.PrimaryLocale);

                        //Grab information about the artist from JPopAsia.com
                        extendedMetadata.JPopAsiaArtistInfo = await ArtistFetcher.FindArtistDataOnJPopAsiaAsync(originalMetadata.Artist.Trim(), station.PrimaryLocale);

                        //Grab a background of the artist from FanArtTV.com
                        extendedMetadata.FanArtTVBackgroundUrl = await FanArtTVFetcher.FetchArtistBackgroundAsync(originalMetadata.Artist.Trim());
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
