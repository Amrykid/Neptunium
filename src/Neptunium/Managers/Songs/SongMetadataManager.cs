using Hqub.MusicBrainz.API.Entities;
using Kukkii;
using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers
{
    public class SongMetadataManager
    {
        public async Task<AlbumData> TryFindAlbumOnMusicBrainzAsync(string track, string artist)
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
                    if (potentialRecording.Title.ToLower().StartsWith(track.ToLower()))
                    {
                        var firstRelease = potentialRecording.Releases.Items.FirstOrDefault();

                        if (firstRelease != null)
                        {

                            //data.AlbumCoverUrl = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();
                            data.AlbumCoverUrl = "http://coverartarchive.org/release/" + firstRelease?.Id + "/front-250.jpg";

                            data.Artist = potentialRecording.Credits.First().Artist.Name;
                            data.ArtistID = potentialRecording.Credits.First().Artist.Id;
                            data.Album = firstRelease.Title;
                            data.AlbumID = firstRelease.Id;
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

        internal async Task<MusicBrainzSongMetadata> GetMusicBrainzDataAsync(string title, string artist)
        {
            MusicBrainzSongMetadata metadata = new MusicBrainzSongMetadata();

            var albumData = await TryFindAlbumOnMusicBrainzAsync(title, artist);
            if (albumData != null)
            {
                metadata.Album = albumData;
            }

            if (albumData != null)
            {
                //get the artist via artist id
                metadata.Artist = await GetArtistOnMusicBrainzAsync(albumData.ArtistID);
            }
            else
            {
                metadata.Artist = await TryFindArtistOnMusicBrainzAsync(artist);
            }

            return metadata;
        }

        public async Task<ArtistData> GetArtistOnMusicBrainzAsync(string artistID)
        {
            ArtistData data = new ArtistData();

            var artistData = await Artist.GetAsync(artistID, "url-rels");

            if (artistData != null)
            {
                data.Name = artistData.Name;
                data.Gender = artistData.Gender;
                data.ArtistID = artistData.Id;

                if (artistData.RelationLists != null)
                {
                    var imageRel = artistData.RelationLists.Items.FirstOrDefault(x => x.Type == "image");
                    if (imageRel != null)
                        data.ArtistImage = imageRel.Target;
                }
            }

            return null;
        }

        public async Task<ArtistData> TryFindArtistOnMusicBrainzAsync(string artistName)
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

                var browsingData = await Artist.GetAsync(artist.Id, "url-rels");

                if (browsingData != null)
                {
                    if (browsingData.RelationLists != null)
                    {
                        var imageRel = browsingData.RelationLists.Items.FirstOrDefault(x => x.Type == "image");
                        if (imageRel != null)
                            data.ArtistImage = imageRel.Target;
                    }
                }

                return data;
            }

            return null;
        }
    }
}
