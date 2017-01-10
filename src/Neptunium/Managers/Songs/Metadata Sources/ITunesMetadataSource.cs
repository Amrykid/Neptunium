using Neptunium.Managers.Songs.Metadata_Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;
using iTunesSearch.Library.Models;

namespace Neptunium.Managers.Songs
{
    public class ITunesMetadataSource : BaseSongMetadataSource
    {
        iTunesSearch.Library.iTunesSearchManager itunesStore = null;
        public ITunesMetadataSource()
        {
            itunesStore = new iTunesSearch.Library.iTunesSearchManager();
        }
        public override Task<ArtistData> GetArtistAsync(string artistID, string locale = "JP")
        {
            throw new NotImplementedException();
        }

        public async override Task<AlbumData> TryFindAlbumAsync(string track, string artist, string locale = "JP")
        {
            //todo pass in station country: i.e. jp or kr

            //todo figure out a better way to do this. maybe romanize the japanese artist names for better accuracy?
            var albumResults = await itunesStore.SearchAlbumsAsync(string.Join(" ", artist, track), 20, locale.ToLower());
            IEnumerable<Album> albums = null;
            if (albumResults.Count != 0)
            {
                albums = albumResults.Albums;
            }
            else
            {
                albumResults = await itunesStore.GetAlbumsFromSongAsync(track, 20, locale.ToLower());
                if (albumResults.Count == 0) return null;

                albums = albumResults.Albums.Where(x =>
                {
                    var artistName = x.ArtistName.Trim();
                    return artistName.FuzzyEquals(artist.Trim(), .75) || artist.Contains(artistName);
                });
            }

            if (albums.Count() > 0)
            {
                Album selectedAlbum = null;

                selectedAlbum = albums.First();
                if (selectedAlbum == null)
                    return null; //give up until we figure out a better way to do this.

                var data = new AlbumData();

                data.Album = selectedAlbum.CollectionName;
                data.AlbumID = selectedAlbum.CollectionId.ToString();

                if (!string.IsNullOrWhiteSpace(selectedAlbum.ArtworkUrl100))
                {
                    string highResImg = selectedAlbum.ArtworkUrl100.Replace("100x100", "600x600");
                    if (await CheckIfUrlIsWebAccessibleAsync(new Uri(highResImg)))
                        data.AlbumCoverUrl = highResImg;
                }
                else if (!string.IsNullOrWhiteSpace(selectedAlbum.ArtworkUrl60))
                {
                    string highResImg = selectedAlbum.ArtworkUrl60.Replace("100x100", "600x600");
                    if (await CheckIfUrlIsWebAccessibleAsync(new Uri(highResImg)))
                        data.AlbumCoverUrl = selectedAlbum.ArtworkUrl60.Replace("100x100", "600x600");
                }

                data.AlbumLinkUrl = selectedAlbum.CollectionViewUrl;
                data.Artist = selectedAlbum.ArtistName;
                data.ArtistID = selectedAlbum.ArtistId.ToString();
                //data.ReleaseDate = selectedAlbum.ReleaseDate;

                return data;
            }

            return null;
        }

        public async override Task<ArtistData> TryFindArtistAsync(string artistName, string locale = "JP")
        {
            var artists = await itunesStore.GetSongArtistsAsync(artistName, 5, locale.ToLower());

            if (artists.Count > 0)
            {
                var selectedArtist = artists.Artists.First();

                var data = new ArtistData();
                data.Name = selectedArtist.ArtistName;
                data.ArtistID = selectedArtist.ArtistId.ToString();
                data.ArtistLinkUrl = selectedArtist.ArtistLinkUrl;

                return data;
            }

            return null;
        }
    }
}
