using Hqub.MusicBrainz.API.Entities;
using Kukkii;
using Neptunium.Data;
using Neptunium.Managers.Songs.Metadata_Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers.Songs
{
    public class SongMetadataManager
    {
        private BaseSongMetadataSource MusicBrainz { get; set; } = new MusicBrainzMetadataSource();
        private BaseSongMetadataSource ITunes { get; set; } = new ITunesMetadataSource();

        internal async Task<MusicBrainzSongMetadata> GetMusicBrainzDataAsync(string track, string artist, string locale = "jp")
        {
            MusicBrainzSongMetadata metadata = new MusicBrainzSongMetadata();

            AlbumData albumData = null;

            try
            {
                albumData = await MusicBrainz.TryFindAlbumAsync(track, artist, locale);
                if (albumData != null)
                {
                    metadata.Album = albumData;
                }
            }
            catch (Hqub.MusicBrainz.API.HttpClientException) { }

            try
            {

                if (albumData != null)
                {
                    //get the artist via artist id
                    metadata.Artist = await MusicBrainz.GetArtistAsync(albumData.ArtistID, locale);
                }
                else
                {
                    metadata.Artist = await MusicBrainz.TryFindArtistAsync(artist, locale);
                }
            }
            catch (Hqub.MusicBrainz.API.HttpClientException) { }

            return metadata;
        }

        internal async Task<ITunesSongMetadata> GetITunesDataAsync(string track, string artist, string locale = "jp")
        {
            ITunesSongMetadata metadata = new ITunesSongMetadata();

            AlbumData albumData = null;

            try
            {
                albumData = await ITunes.TryFindAlbumAsync(track, artist, locale);
                if (albumData != null)
                {
                    metadata.Album = albumData;
                }
            }
            catch (Hqub.MusicBrainz.API.HttpClientException) { }
            catch (NotImplementedException) { }

            try
            {
                //if (albumData != null)
                //{
                //    //get the artist via artist id
                //    metadata.Artist = await ITunes.GetArtistAsync(albumData.ArtistID);
                //}
                //else
                //{
                metadata.Artist = await ITunes.TryFindArtistAsync(artist, locale);
                //}
            }
            catch (Hqub.MusicBrainz.API.HttpClientException) { }
            catch (NotImplementedException) { }

            return metadata;
        }
    }
}
