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
        internal static async Task<AlbumData> FindAlbumDataAsync(string title, string artist)
        {
            string cleanedArtist = artist.Trim();
            string cleanedTrack = title.Trim();

            string key = cleanedArtist + "|" + cleanedTrack;

            if (await CookieJar.DeviceCache.ContainsObjectAsync(key))
                return await CookieJar.DeviceCache.PeekObjectAsync<AlbumData>(key);

            try
            {
                var albumData = await TryFindArtistOnMusicBrainzAsync(cleanedTrack, cleanedArtist);

                if (albumData != null)
                {
                    await CookieJar.DeviceCache.InsertObjectAsync<AlbumData>(key, albumData);
                    await CookieJar.DeviceCache.FlushAsync();

                    FoundMetadata?.Invoke(null, new SongMetadataManagerFoundMetadataEventArgs() { FoundAlbumData = albumData, QueriedTrack = cleanedTrack, QueiredArtist = cleanedArtist });
                }

                return albumData;
            }
            catch (Exception) { }

            return null;
        }

        private static async Task<AlbumData> TryFindArtistOnMusicBrainzAsync(string track, string artist)
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
                                data.AlbumCoverUrl = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();
                            }
                            catch (Exception) { }

                            data.Artist = potentialRecording.Credits.First().Artist.Name;
                            data.ArtistID = potentialRecording.Credits.First().Artist.Id;
                            data.Album = firstRelease.Title;
                            data.AlbumID = firstRelease.Id;
                            if (!string.IsNullOrWhiteSpace(firstRelease.Date))
                                data.ReleaseDate = DateTime.Parse(firstRelease.Date);

                            return data;
                        }
                    }
                }

            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public static event EventHandler<SongMetadataManagerFoundMetadataEventArgs> FoundMetadata;
    }
}
