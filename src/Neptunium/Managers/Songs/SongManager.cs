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
using System.Threading;
using System.Threading.Tasks;
using UWPShoutcastMSS.Streaming;
using Windows.Storage;

namespace Neptunium.Managers.Songs
{
    public static class SongManager
    {
        internal static SongHistoryManager HistoryManager { get; private set; }
        internal static SongMetadataManager MetadataManager { get; private set; }

        public static SongMetadata CurrentSong { get; private set; }
        public static bool IsInitialized { get; private set; }

        private static SemaphoreSlim metadataChangeLock = new SemaphoreSlim(1);


        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;

            HistoryManager = new SongHistoryManager();
            await HistoryManager.InitializeAsync();

            MetadataManager = new SongMetadataManager();

            IsInitialized = true;
        }

        private static async void StationMediaPlayer_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
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

            await metadataChangeLock.WaitAsync();

            if (e.Title.ToLower().Equals("unknown song") || e.Artist.ToLower().Equals("unknown artist"))
            {
                SongMetadata tmp = new UnknownSongMetadata();
                tmp.Track = e.Title.Trim();
                tmp.Artist = e.Artist.Trim();

                PreSongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
                {
                    Metadata = tmp,
                    IsUnknown = true
                });

                CurrentSong = tmp;

                SongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
                {
                    Metadata = tmp,
                    IsUnknown = true
                });
            }
            else
            {
                //do preprocessing here.
                SongMetadata metadata = new SongMetadata();
                metadata.Track = e.Title.Trim();
                metadata.Artist = e.Artist.Trim();

                PreSongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
                {
                    Metadata = metadata,
                });

                CurrentSong = metadata;

                string storageKey = "SONG|" + metadata.GetHashCode();

                bool cachedSong = false;
                if (cachedSong = await CookieJar.DeviceCache.ContainsObjectAsync(storageKey))
                {
                    metadata = await CookieJar.DeviceCache.PeekObjectAsync<SongMetadata>(storageKey);
                }
                else
                {
                    if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.TryToFindSongMetadata] == true)
                    {
                        string cleanArtist = e.Artist; //strip out featured artist
                                                       //cleanArtist = Regex.Replace(cleanArtist, "[fF][t(eat(turing))].*", "").Trim();
                                                       //Fukimaki Ryota matches and gets removed.

                        try
                        {
                            metadata.MBData = await MetadataManager.GetMusicBrainzDataAsync(e.Title, cleanArtist, StationMediaPlayer.CurrentStation.PrimaryLocale);
                        }
                        catch (NotImplementedException)
                        {

                        }

                        if (metadata.MBData == null || metadata.MBData?.Album == null || metadata.MBData?.Artist == null)
                        {
                            try
                            {
                                metadata.ITunesData = await MetadataManager.GetITunesDataAsync(e.Title, cleanArtist, StationMediaPlayer.CurrentStation.PrimaryLocale);
                            }
                            catch (NotImplementedException)
                            {

                            }
                        }
                    }
                }

                CurrentSong = metadata;

                SongChanged?.Invoke(null, new SongManagerSongChangedEventArgs()
                {
                    Metadata = metadata,
                });

                HistoryManager.HandleNewSongPlayed(metadata, StationMediaPlayer.CurrentStation);

                if (!cachedSong)
                {
                    await CookieJar.DeviceCache.InsertObjectAsync<SongMetadata>(storageKey, metadata,
                        (int)TimeSpan.FromDays(15).TotalMilliseconds);
                    await CookieJar.DeviceCache.FlushAsync();
                }
            }

            metadataChangeLock.Release();
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

            if (metadata.MBData != null)
            {
                if (metadata.MBData.Album != null)
                {
                    if (!string.IsNullOrWhiteSpace(metadata.MBData.Album.AlbumCoverUrl))
                        return new Uri(metadata.MBData.Album.AlbumCoverUrl);
                }
                else if (metadata.MBData.Artist != null)
                {
                    if (!string.IsNullOrWhiteSpace(metadata.MBData.Artist.ArtistImage))
                        return new Uri(metadata.MBData.Artist.ArtistImage);
                }
            }
            else if (metadata.ITunesData != null)
            {
                if (metadata.ITunesData.Album != null)
                {
                    if (!string.IsNullOrWhiteSpace(metadata.ITunesData.Album.AlbumCoverUrl))
                        return new Uri(metadata.ITunesData.Album.AlbumCoverUrl);
                }
            }

            try
            {
                var musicBrainzData = await MetadataManager.GetMusicBrainzDataAsync(title, artist);
                var itunesData = await MetadataManager.GetITunesDataAsync(title, artist);

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

                        if (artistData != null && !string.IsNullOrWhiteSpace(artistData?.ArtistImage))
                        {
                            return new Uri(artistData?.ArtistImage);
                        }
                        else
                        {
                            TraceTelemetry trace = new TraceTelemetry("MusicBrainz - Failed song data lookup.", Microsoft.HockeyApp.SeverityLevel.Information);
                            trace.Properties.Add(new KeyValuePair<string, string>("Artist", artist));
                            trace.Properties.Add(new KeyValuePair<string, string>("Song", title));
                            Microsoft.HockeyApp.HockeyClient.Current.TrackTrace(trace);
                        }
                    }
                }

                if (itunesData != null)
                {
                    if (itunesData.Album != null)
                    {
                        if (!string.IsNullOrWhiteSpace(itunesData.Album.AlbumCoverUrl))
                            return new Uri(itunesData.Album.AlbumCoverUrl);
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
