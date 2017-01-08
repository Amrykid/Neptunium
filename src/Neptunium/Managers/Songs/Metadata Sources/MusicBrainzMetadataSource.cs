using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;
using Hqub.MusicBrainz.API.Entities;

namespace Neptunium.Managers.Songs.Metadata_Sources
{
    public class MusicBrainzMetadataSource : BaseSongMetadataSource
    {
        public async override Task<ArtistData> GetArtistAsync(string artistID)
        {
            ArtistData data = new ArtistData();

            var artistData = await Artist.GetAsync(artistID, "url-rels", "aliases");

            if (artistData != null)
            {
                data.Name = artistData.Name;
                data.Gender = artistData.Gender;
                data.ArtistID = artistData.Id;
                data.ArtistLinkUrl = "https://musicbrainz.org/artist/" + artistData.Id;

                if (artistData.RelationLists != null)
                {
                    var imageRel = artistData.RelationLists.Items.FirstOrDefault(x => x.Type == "image");
                    if (imageRel != null)
                    {
                        if (!string.IsNullOrWhiteSpace(imageRel.Target))
                        {
                            if (await CheckIfUrlIsWebAccessibleAsync(new Uri(imageRel.Target)))
                                data.ArtistImage = imageRel.Target;
                        }
                    }
                }

                return data;
            }

            return null;
        }

        public async override Task<AlbumData> TryFindAlbumAsync(string track, string artist)
        {
            AlbumData data = new AlbumData();


            var recordingQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Recording>();
            recordingQuery.Add("artistname", artist);
            recordingQuery.Add("country", "JP");
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

        public async override Task<ArtistData> TryFindArtistAsync(string artistName)
        {
            ArtistData data = new ArtistData();

            var artistQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Artist>();
            //artistQuery.Add("inc", "url-rels");
            artistQuery.Add("artist", artistName);
            artistQuery.Add("alias", artistName);
            artistQuery.Add("country", "JP");

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
    }
}
