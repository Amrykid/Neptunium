using Neptunium.Core.Media.Metadata;
using System;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Storage.Streams;

namespace Neptunium.Media.Songs
{
    public class NepAppSongManagerMediaTransportUpdater
    {
        public NepAppSongManagerMediaTransportUpdater(NepAppSongManager songManager)
        {
            songManager.PreSongChanged += SongManager_PreSongChanged;
            songManager.SongChanged += SongManager_SongChanged;
            songManager.ArtworkProcessor.NoSongArtworkAvailable += SongManager_NoSongArtworkAvailable;
            songManager.ArtworkProcessor.SongArtworkAvailable += SongManager_SongArtworkAvailable;
        }

        private void SongManager_SongArtworkAvailable(object sender, NepAppSongMetadataArtworkEventArgs e)
        {
            var updater = NepApp.MediaPlayer.MediaTransportControls.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;

            //update artwork for song metadata
            RandomAccessStreamReference uriStream = null;
            NepAppSongMetadataBackground artworkType = NepAppSongMetadataBackground.Artist;
            if (NepApp.SongManager.ArtworkProcessor.IsSongArtworkAvailable(out artworkType) && artworkType == NepAppSongMetadataBackground.Album)
            {
                uriStream = RandomAccessStreamReference.CreateFromUri(NepApp.SongManager.ArtworkProcessor.GetSongArtworkUri(NepAppSongMetadataBackground.Album));
            }
            else
            {
                uriStream = RandomAccessStreamReference.CreateFromUri(e.CurrentMetadata.StationLogo);
            }
            if (uriStream != null)
            {
                updater.Thumbnail = uriStream;
            }


            updater.Update();
        }

        private void SongManager_NoSongArtworkAvailable(object sender, NepAppSongMetadataArtworkEventArgs e)
        {
            var updater = NepApp.MediaPlayer.MediaTransportControls.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;

            updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(e.CurrentMetadata.StationLogo);

            updater.Update();
        }

        private void SongManager_SongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            UpdateTransportControls(e.Metadata);
        }

        private void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            UpdateTransportControls(e.Metadata);
        }

        private void UpdateTransportControls(SongMetadata songMetadata)
        {
            if (songMetadata == null) return;

            try
            {
                var updater = NepApp.MediaPlayer.MediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = songMetadata.Track;
                updater.MusicProperties.Artist = songMetadata.Artist;
                updater.AppMediaId = songMetadata.StationPlayedOn.GetHashCode().ToString();

                if (songMetadata.StationLogo != null)
                {
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(songMetadata.StationLogo);
                }

                if (songMetadata is ExtendedSongMetadata)
                {
                    var extended = (ExtendedSongMetadata)songMetadata;

                    //add album title and album artist information if it is available
                    if (extended.Album != null)
                    {
                        updater.MusicProperties.AlbumTitle = extended.Album?.Album ?? "";
                        updater.MusicProperties.AlbumArtist = extended.Album?.Artist ?? "";
                    }
                }
                else
                {
                    updater.MusicProperties.AlbumTitle = "";
                    updater.MusicProperties.AlbumArtist = "";
                }

                updater.Update();
            }
            catch (COMException) { }
            catch (Exception)
            {

            }
        }
    }
}
