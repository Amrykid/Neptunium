using Crystal3.Core;
using Kukkii;
using Microsoft.HockeyApp.DataContracts;
using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace Neptunium.Managers.Songs
{
    public static class SongManager
    {
        internal static SongHistoryManager HistoryManager { get; private set; }
        internal static SongMetadataManager MetadataManager { get; private set; }

        public static SongMetadata CurrentSong { get; private set; }
        public static bool IsInitialized { get; private set; }


        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;

            HistoryManager = new SongHistoryManager();
            await HistoryManager.InitializeAsync();

            MetadataManager = new SongMetadataManager();

            IsInitialized = true;
        }

        private static async void StationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Contains(e.Title)) return; //don't play that pre-defined station message that happens every so often.


            if (!string.IsNullOrWhiteSpace(e.Title) && string.IsNullOrWhiteSpace(e.Artist))
            {
                //station message got through.

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#else
                return;
#endif
            }

            //do preprocessing here.
            SongMetadata metadata = new SongMetadata();
            metadata.Track = e.Title.Trim();
            metadata.Artist = e.Artist.Trim();

            PreSongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
            {
                Metadata = metadata,
            });

            string storageKey = "SONG|" + metadata.GetHashCode();

            bool cachedSong = false;
            if (cachedSong = await CookieJar.DeviceCache.ContainsObjectAsync(storageKey))
                metadata = await CookieJar.DeviceCache.PeekObjectAsync<SongMetadata>(storageKey);
            else
            {
                if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.TryToFindSongMetadata] == true)
                {
                    string cleanArtist = e.Artist; //strip out featured artist
                    cleanArtist = Regex.Replace(cleanArtist, "[fF][t(eat(turing))].*", "");

                    try
                    {
                        metadata.MBData = await MetadataManager.GetMusicBrainzDataAsync(e.Title, cleanArtist);
                    }
                    catch (NotImplementedException)
                    {

                    }

                    try
                    {
                        metadata.ITunesData = await MetadataManager.GetITunesDataAsync(e.Title, cleanArtist);
                    }
                    catch (NotImplementedException)
                    {

                    }
                }
            }

            CurrentSong = metadata;

            SongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
            {
                Metadata = metadata,
            });

            if (!cachedSong)
            {
                await CookieJar.DeviceCache.InsertObjectAsync<SongMetadata>(storageKey, metadata, 
                    (int)TimeSpan.FromDays(15).TotalMilliseconds);
                await CookieJar.DeviceCache.FlushAsync();
            }
        }

        public static event EventHandler<SongManagerSongChangedEventArgs> PreSongChanged;
        public static event EventHandler<SongManagerSongChangedEventArgs> SongChanged;

        internal static Task FlushAsync()
        {
            // throw new NotImplementedException();
            return Task.CompletedTask;
        }

        internal static async Task<Uri> GetSongBackgroundAsync(SongMetadata metadata)
        {
            string title = metadata.Track.Trim();
            string artist = metadata.Artist.Trim();

            try
            {
                var musicBrainzData = await MetadataManager.GetMusicBrainzDataAsync(title, artist);

                if (musicBrainzData != null)
                {
                    var albumData = musicBrainzData.Album;

                    if (albumData != null && !string.IsNullOrWhiteSpace(albumData?.AlbumCoverUrl))
                    {
                        return new Uri(albumData?.AlbumCoverUrl);
                    }
                    else
                    {
                        var artistData = musicBrainzData.Artist;

                        if (artistData != null && !string.IsNullOrWhiteSpace(artistData?.ArtistID))
                        {
                            return new Uri(artistData?.ArtistImage);
                        }
                        else
                        {
                            TraceTelemetry trace = new TraceTelemetry("Failed song data lookup.", Microsoft.HockeyApp.SeverityLevel.Information);
                            trace.Properties.Add(new KeyValuePair<string, string>("Artist", artist));
                            trace.Properties.Add(new KeyValuePair<string, string>("Song", title));
                            Microsoft.HockeyApp.HockeyClient.Current.TrackTrace(trace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dictionary<string, string> info = new Dictionary<string, string>();
                info.Add("Message", "Failed song data lookup.");
                info.Add("Artist", artist);
                info.Add("Song", title);
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, info);
            }

            return null;
        }
    }
}
