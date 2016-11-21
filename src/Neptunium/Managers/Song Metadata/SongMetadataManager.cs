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
    public static class SongMetadataManager
    {
        internal static async Task<ArtistData> FindArtistDataAsync(string artist)
        {
            string cleanedArtist = artist.Trim();
            string key = "ARTIST:" + cleanedArtist;

            if (await CookieJar.DeviceCache.ContainsObjectAsync(key))
                await CookieJar.DeviceCache.PeekObjectAsync<ArtistData>(key);

            try
            {
                var artistData = await TryFindArtistOnMusicBrainzAsync(cleanedArtist);

                if (artistData != null)
                {
                    await CookieJar.DeviceCache.InsertObjectAsync<ArtistData>(key, artistData);

                    await CookieJar.DeviceCache.FlushAsync();

                    FoundArtistMetadata?.Invoke(null, new SongMetadataManagerFoundArtistMetadataEventArgs() { });
                }

                return artistData;
            }
            catch (Exception)
            {

            }

            return null;
        }
        internal static async Task<AlbumData> FindAlbumDataAsync(string title, string artist)
        {
            string cleanedArtist = artist.Trim();
            string cleanedTrack = title.Trim();

            string key = "ALBUM:" + cleanedArtist + "|" + cleanedTrack;

            if (await CookieJar.DeviceCache.ContainsObjectAsync(key))
                return await CookieJar.DeviceCache.PeekObjectAsync<AlbumData>(key);

            try
            {
                var albumData = await TryFindAlbumOnMusicBrainzAsync(cleanedTrack, cleanedArtist);

                if (albumData != null)
                {
                    await CookieJar.DeviceCache.InsertObjectAsync<AlbumData>(key, albumData);

                    if (!await CookieJar.DeviceCache.ContainsObjectAsync("ARTIST:" + albumData.Artist))
                        await CookieJar.DeviceCache.InsertObjectAsync("ARTIST:" + albumData.Artist, new ArtistData() { Name = albumData.Artist, ArtistID = albumData.ArtistID });

                    await CookieJar.DeviceCache.FlushAsync();

                    FoundAlbumMetadata?.Invoke(null, new SongMetadataManagerFoundAlbumMetadataEventArgs() { FoundAlbumData = albumData, QueriedTrack = cleanedTrack, QueiredArtist = cleanedArtist });
                }

                return albumData;
            }
            catch (Exception) { }

            return null;
        }

        private static async Task<AlbumData> TryFindAlbumOnMusicBrainzAsync(string track, string artist)
        {
            AlbumData data = new AlbumData();
            try
            {
                var recordingQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Recording>();
                recordingQuery.Add("artistname", artist);
                recordingQuery.Add("country", "JP");
                recordingQuery.Add("recording", track);

                var recordings = await Recording.SearchAsync(recordingQuery);

                foreach (var potentialRecording in recordings?.Items)
                {
                    if (potentialRecording.Title.ToLower().StartsWith(track.ToLower()))
                    {
                        var firstRelease = potentialRecording.Releases.Items.FirstOrDefault();

                        if (firstRelease != null)
                        {
                            try
                            {
                                //data.AlbumCoverUrl = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();
                                data.AlbumCoverUrl = "http://coverartarchive.org/release/" + firstRelease.Id + "/front-250.jpg";
                            }
                            catch (Exception) { }

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
            catch (Exception ex)
            {
                return null;
            }

            return null;
        }

        private static async Task<ArtistData> TryFindArtistOnMusicBrainzAsync(string artistName)
        {
            ArtistData data = new ArtistData();
            try
            {
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

                    await Task.Delay(200);

                    try
                    {
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
                    }
                    catch (Exception) { }

                    return data;
                }

            }
            catch (Exception)
            {

            }

            return null;
        }

        public static event EventHandler<SongMetadataManagerFoundAlbumMetadataEventArgs> FoundAlbumMetadata;
        public static event EventHandler<SongMetadataManagerFoundArtistMetadataEventArgs> FoundArtistMetadata;
    }
}
