using Hqub.MusicBrainz.API.Entities;
using Neptunium.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Metadata
{
    /// <summary>
    /// Defines the metadata source for MusicBrainz.
    /// </summary>
    public class MusicBrainzMetadataSource : BaseSongMetadataSource
    {
        /// <summary>
        /// Gets an artist using a previously retrieved ID.
        /// </summary>
        /// <param name="artistID">The ID of the artist to retrieve.</param>
        /// <param name="locale">The locale of the artist which the ID corresponds to.</param>
        /// <returns>ArtistData or null</returns>
        public async override Task<ArtistData> GetArtistAsync(string artistID, string locale = "JP")
        {
            ArtistData data = new ArtistData();

            var artistData = await Artist.GetAsync(artistID, "url-rels", "aliases", "artist-rels");

            if (artistData != null)
            {
                data.Name = artistData.Name;

                data.Gender = artistData.Gender;

                data.ArtistID = artistData.Id;
                data.Country = artistData.Country;

                data.ArtistLinkUrl = "https://musicbrainz.org/artist/" + artistData.Id;


                if (artistData.RelationLists != null)
                {
                    var imageRel = artistData.RelationLists.Items?.FirstOrDefault(x => x.Type == "image");

                    if (imageRel != null)
                    {
                        if (!string.IsNullOrWhiteSpace(imageRel.Target))
                        {
                            if (await CheckIfUrlIsWebAccessibleAsync(new Uri(imageRel.Target)))
                                data.ArtistImage = imageRel.Target;
                        }
                    }

                    var wikipediaRel = artistData.RelationLists.Items?.FirstOrDefault(x => x.Type == "wikipedia");

                    if (wikipediaRel != null)
                    {
                        data.WikipediaUrl = wikipediaRel.Target;
                    }
                }

                return data;
            }

            return null;
        }

        /// <summary>
        /// Tries to find an album corresponding to a particular track, artist and locale.
        /// </summary>
        /// <param name="track">The track or song that is contained on the album.</param>
        /// <param name="artist">The artist who released the album.</param>
        /// <param name="locale">The locale of the artist when they released the album.</param>
        /// <returns>AlbumData or null</returns>
        public async override Task<AlbumData> TryFindAlbumAsync(string track, string artist, string locale = "JP")
        {
            AlbumData data = new AlbumData();

            var recordingQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Recording>();

            recordingQuery.Add("artistname", artist);

            recordingQuery.Add("country", locale);

            recordingQuery.Add("recording", track);

            var recordings = await Recording.SearchAsync(recordingQuery);

            if (recordings?.QueryCount > 0)
            {
                foreach (var potentialRecording in recordings?.Items)
                {
                    if (potentialRecording.Title.ToLower().StartsWith(track.ToLower()) || potentialRecording.Title.ToLower().Trim().FuzzyEquals(track.ToLower().Trim()))
                    {
                        var firstRelease = potentialRecording.Releases.Items.FirstOrDefault();

                        if (firstRelease != null)
                        {
                            //data.AlbumCoverUrl = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();

                            if (firstRelease.CoverArtArchive != null)
                            {
                                if (firstRelease.CoverArtArchive.Artwork)
                                {
                                    string albumImg = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();
                                    if (!string.IsNullOrWhiteSpace(albumImg))
                                    {
                                        if (await CheckIfUrlIsWebAccessibleAsync(new Uri(albumImg)))
                                            data.AlbumCoverUrl = albumImg;
                                    }
                                }
                            }
                            else
                            {
                                string albumImg = "http://coverartarchive.org/release/" + firstRelease?.Id + "/front-250.jpg";

                                if (!string.IsNullOrWhiteSpace(albumImg))
                                {
                                    if (await CheckIfUrlIsWebAccessibleAsync(new Uri(albumImg)))
                                        data.AlbumCoverUrl = albumImg;
                                }
                            }

                            data.Artist = potentialRecording.Credits.First().Artist.Name;

                            data.ArtistID = potentialRecording.Credits.First().Artist.Id;

                            data.Album = firstRelease.Title;

                            data.AlbumID = firstRelease.Id;

                            data.AlbumLinkUrl = "https://musicbrainz.org/release/" + firstRelease.Id;

                            if (!string.IsNullOrWhiteSpace(firstRelease.Date))
                            {
                                try
                                {
                                    data.ReleaseDate = DateTime.Parse(firstRelease.Date);
                                }
                                catch (FormatException) { }
                            }
                            return data;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to find an artist by their name and locale.
        /// </summary>
        /// <param name="artistName">The artist's name.</param>
        /// <param name="locale">The locale of the artist which is used to narrow the search.</param>
        /// <returns>ArtistData or null</returns>
        public async override Task<ArtistData> TryFindArtistAsync(string artistName, string locale = "JP")
        {
            ArtistData data = new ArtistData();

            var artistQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Artist>();

            //artistQuery.Add("inc", "url-rels");

            artistQuery.Add("artist", artistName);

            artistQuery.Add("alias", artistName);

            artistQuery.Add("country", locale);

            var artistResults = await Artist.SearchAsync(artistQuery);

            var artist = artistResults?.Items.FirstOrDefault();

            if (artist != null)
            {
                data.Name = artist.Name;

                data.Gender = artist.Gender;

                data.ArtistID = artist.Id;

                data.ArtistLinkUrl = "https://musicbrainz.org/artist/" + artist.Id;

                var browsingData = await Artist.GetAsync(artist.Id, "url-rels", "aliases");

                if (browsingData != null)
                {
                    if (browsingData.RelationLists != null)
                    {
                        var imageRel = browsingData.RelationLists.Items.FirstOrDefault(x => x.Type == "image");

                        if (imageRel != null)
                        {
                            if (!string.IsNullOrWhiteSpace(imageRel.Target))
                            {
                                if (await CheckIfUrlIsWebAccessibleAsync(new Uri(imageRel.Target)))
                                    data.ArtistImage = imageRel.Target;
                            }
                        }
                    }
                }

                return data;
            }

            return null;
        }

        /// <summary>
        /// Tries to find information on a particular song.
        /// </summary>
        /// <param name="song">The song to search for.</param>
        /// <param name="locale">The locale for narrowing the search.</param>
        /// <returns></returns>
        public async override Task TryFindSongAsync(ExtendedSongMetadata song, string locale = "JP")
        {
            var recordingQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Recording>();

            //artistQuery.Add("inc", "url-rels");

            recordingQuery.Add("artist", song.Artist);

            recordingQuery.Add("recording", song.Track);

            recordingQuery.Add("country", locale);

            var recordingResults = await Recording.SearchAsync(recordingQuery);

            var recording = recordingResults?.Items.FirstOrDefault();

            if (recording != null)
            {
                song.SongLength = TimeSpan.FromMilliseconds(recording.Length);
            }
        }
    }
}
