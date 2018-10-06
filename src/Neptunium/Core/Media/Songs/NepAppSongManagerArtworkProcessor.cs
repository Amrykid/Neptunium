using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Media.Songs
{
    public class NepAppSongManagerArtworkProcessor
    {
        private Dictionary<NepAppSongMetadataBackground, Uri> artworkUriDictionary = null;

        public event EventHandler<NepAppSongMetadataArtworkEventArgs> SongArtworkAvailable;
        public event EventHandler<NepAppSongMetadataArtworkEventArgs> NoSongArtworkAvailable;
        public event EventHandler SongArtworkProcessingComplete;

        public NepAppSongManagerArtworkProcessor(NepAppSongManager songManager)
        {
            artworkUriDictionary = new Dictionary<NepAppSongMetadataBackground, Uri>();
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Album, null);
            artworkUriDictionary.Add(NepAppSongMetadataBackground.Artist, null);
        }

        public Uri GetSongArtworkUri(NepAppSongMetadataBackground nepAppSongMetadataBackground)
        {
            return artworkUriDictionary[nepAppSongMetadataBackground];
        }

        public bool IsSongArtworkAvailable()
        {
            return IsSongArtworkAvailable(out NepAppSongMetadataBackground type);
        }

        public bool IsSongArtworkAvailable(out NepAppSongMetadataBackground nepAppSongMetadataBackgroundType)
        {
            if (artworkUriDictionary[NepAppSongMetadataBackground.Album] != null)
            {
                nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.Album;
                return true;
            }
            else if (artworkUriDictionary[NepAppSongMetadataBackground.Artist] != null)
            {
                nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.Artist;
                return true;
            }

            nepAppSongMetadataBackgroundType = NepAppSongMetadataBackground.None;
            return false;
        }

        internal void UpdateArtworkMetadata()
        {
            var currentSong = NepApp.SongManager.CurrentSong;
            var currentSongWithMetadata = NepApp.SongManager.CurrentSongWithAdditionalMetadata;

            //Handle backgrounds
            if (currentSongWithMetadata != null)
            {
                //album artwork
                Uri albumArtUri = null;

                if (currentSongWithMetadata.FanArtTVBackgroundUrl != null)
                {
                    albumArtUri = currentSongWithMetadata.FanArtTVBackgroundUrl;
                }
                if (currentSongWithMetadata.Album != null && albumArtUri == null)
                {
                    if (!string.IsNullOrWhiteSpace(currentSongWithMetadata.Album?.AlbumCoverUrl))
                    {
                        albumArtUri = new Uri(currentSongWithMetadata.Album?.AlbumCoverUrl);
                    }
                }

                artworkUriDictionary[NepAppSongMetadataBackground.Album] = albumArtUri;
                if (albumArtUri != null)
                {
                    SongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, albumArtUri, currentSong));
                }
                else
                {
                    NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, null, currentSong));
                }


                //artist artwork
                Uri artistArtUri = null;
                if (!string.IsNullOrWhiteSpace(currentSongWithMetadata.ArtistInfo?.ArtistImage))
                {
                    artistArtUri = new Uri(currentSongWithMetadata.ArtistInfo?.ArtistImage);
                }
                else if (currentSongWithMetadata.JPopAsiaArtistInfo != null)
                {
                    //from JPopAsia
                    if (currentSongWithMetadata.JPopAsiaArtistInfo.ArtistImageUrl != null)
                    {
                        artistArtUri = currentSongWithMetadata.JPopAsiaArtistInfo.ArtistImageUrl;
                    }
                }
                artworkUriDictionary[NepAppSongMetadataBackground.Artist] = artistArtUri;
                if (artistArtUri != null)
                {
                    SongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, artistArtUri, currentSong));
                }
                else
                {
                    NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, null, currentSong));
                }
            }
            else
            {
                //reset all artwork
                artworkUriDictionary[NepAppSongMetadataBackground.Album] = null;
                artworkUriDictionary[NepAppSongMetadataBackground.Artist] = null;

                NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Album, null, currentSong));
                NoSongArtworkAvailable?.Invoke(this, new NepAppSongMetadataArtworkEventArgs(NepAppSongMetadataBackground.Artist, null, currentSong));
            }

            SongArtworkProcessingComplete?.Invoke(this, EventArgs.Empty);
        }
    }
}
