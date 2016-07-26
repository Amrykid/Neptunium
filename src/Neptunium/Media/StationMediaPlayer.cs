using Neptunium.Data;
using Neptunium.MediaSourceStream;
using Neptunium.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Media;
using System.Diagnostics;
using System.Threading;
using Windows.Web.Http;
using Windows.Storage.Streams;

namespace Neptunium.Media
{
    public static class StationMediaPlayer
    {
        private static ShoutcastMediaSourceStream currentStationMSSWrapper = null;
        private static string currentTrack = "Title";
        private static string currentArtist = "Artist";
        private static StationModelStreamServerType currentStationServerType;

        private static StationModel currentStationModel = null;
        private static SystemMediaTransportControls smtc;

        private static SemaphoreSlim playStationResetEvent = new SemaphoreSlim(1);


        public static bool IsInitialized { get; private set; }

        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            // BackgroundMediaPlayer.Current.

            smtc = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged += Smtc_PropertyChanged;


            smtc.IsChannelDownEnabled = false;
            smtc.IsChannelUpEnabled = false;
            smtc.IsFastForwardEnabled = false;
            smtc.IsNextEnabled = false;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsPreviousEnabled = false;
            smtc.IsRecordEnabled = false;
            smtc.IsRewindEnabled = false;
            smtc.IsStopEnabled = false;

            IsInitialized = true;

            await Task.CompletedTask;
        }

        public static void Deinitialize()
        {
            if (!IsInitialized) return;

            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;

            IsInitialized = false;
        }

        private static void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            switch (args.Property)
            {
                case SystemMediaTransportControlsProperty.SoundLevel:
                    break;
            }
        }

        private static void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Play();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");

                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");

                    break;
            }
        }

        public static ShoutcastSongInfo SongMetadata { get; private set; }

        public static StationModel CurrentStation { get { return currentStationModel; } }

        public static bool IsPlaying
        {
            get
            {
                var state = BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState;

                return state == MediaPlaybackState.Opening || state == MediaPlaybackState.Playing || state == MediaPlaybackState.Buffering;
            }
        }

        //public static ShoutcastStationInfo StationInfoFromStream { get { return currentStationMSSWrapper?.StationInfo; } }

        public static async Task<bool> PlayStationAsync(StationModel station)
        {
            if (station == currentStationModel && IsPlaying) return true;

            await playStationResetEvent.WaitAsync();

            ShoutcastMediaSourceStream lastStream = null;

            if (IsPlaying)
            {
                if (currentStationMSSWrapper != null && (currentStationServerType == StationModelStreamServerType.Shoutcast || currentStationServerType == StationModelStreamServerType.Icecast))
                {
                    currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                    if (currentStationMSSWrapper.MediaStreamSource != null)
                        currentStationMSSWrapper.MediaStreamSource.Closed -= MediaStreamSource_Closed;

                    BackgroundMediaPlayer.Current.Pause();
                }
            }

            if (ConnectingStatusChanged != null)
                ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(true));

            currentStationModel = station;

            //TODO use a combo of events+anon-delegates and TaskCompletionSource to detect play back errors here to seperate connection errors from long-running audio errors.
            //handle error when connecting.

            var stream = station.Streams.First();

            currentStationServerType = stream.ServerType;

            if (currentStationServerType == StationModelStreamServerType.Direct)
            {
                try
                {
                    BackgroundMediaPlayer.Current.SetUriSource(new Uri(stream.Url));

                    BackgroundMediaPlayer.Current.Play();

                    currentTrack = "Unknown Song";
                    currentArtist = "Unknown Artist";

                    UpdateNowPlaying(currentTrack, currentArtist);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if ((currentStationServerType == StationModelStreamServerType.Shoutcast || currentStationServerType == StationModelStreamServerType.Icecast))
            {
                currentStationMSSWrapper = new ShoutcastMediaSourceStream(new Uri(stream.Url));

                currentStationMSSWrapper.MetadataChanged += CurrentStationMSSWrapper_MetadataChanged;


                try
                {
                    if (await currentStationMSSWrapper.ConnectAsync(stream.SampleRate, stream.RelativePath) != null)
                    {
                        BackgroundMediaPlayer.Current.SetMediaSource(currentStationMSSWrapper.MediaStreamSource);

                        BackgroundMediaPlayer.Current.Play();

                        currentStationMSSWrapper.MediaStreamSource.Closed += MediaStreamSource_Closed;

                        currentTrack = "Unknown Song";
                        currentArtist = "Unknown Artist";

                        //UpdateNowPlaying(currentTrack, currentArtist);
                        SongMetadata = null;

                        if (CurrentStationChanged != null) CurrentStationChanged(null, EventArgs.Empty);
                    }
                    else
                    {
                        currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                        if (currentStationMSSWrapper.MediaStreamSource != null)
                            currentStationMSSWrapper.MediaStreamSource.Closed -= MediaStreamSource_Closed;
                    }
                }
                catch (Exception)
                {
                    currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                    if (currentStationMSSWrapper.MediaStreamSource != null)
                        currentStationMSSWrapper.MediaStreamSource.Closed -= MediaStreamSource_Closed;

                    if (BackgroundAudioError != null) BackgroundAudioError(null, EventArgs.Empty);
                }

                if (lastStream != null) lastStream.Disconnect();
            }

            if (ConnectingStatusChanged != null)
                ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(false));

            playStationResetEvent.Release();

            if (IsPlaying)
                UpdateThumbnail(station);

            return IsPlaying;
        }

        private static void UpdateThumbnail(StationModel station)
        {
            if (!string.IsNullOrWhiteSpace(station.Logo))
            {
                try
                {
                    smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(station.Logo));
                    smtc.DisplayUpdater.Update();
                }
                catch (Exception) { }
            }
        }

        private static void MediaStreamSource_Closed(Windows.Media.Core.MediaStreamSource sender, Windows.Media.Core.MediaStreamSourceClosedEventArgs args)
        {
            switch (args.Request.Reason)
            {
                case Windows.Media.Core.MediaStreamSourceClosedReason.Done:
                    return;
                default:
                    if (BackgroundAudioError != null) BackgroundAudioError(null, EventArgs.Empty);
                    break;
            }
        }

        private static void CurrentStationMSSWrapper_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            currentArtist = e.Artist;
            currentTrack = e.Title;

            UpdateNowPlaying(e.Title, e.Artist);
        }

        private static void UpdateNowPlaying(string currentTrack, string currentArtist)
        {
            SongMetadata = new ShoutcastSongInfo() { Track = currentTrack, Artist = currentArtist };

            try
            {
                smtc.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                smtc.DisplayUpdater.MusicProperties.Title = currentTrack;
                smtc.DisplayUpdater.MusicProperties.Artist = currentArtist;

                smtc.DisplayUpdater.AppMediaId = currentStationModel.Name;

                smtc.DisplayUpdater.Update();
            }
            catch (Exception) { }

            if (MetadataChanged != null)
                MetadataChanged(null, new ShoutcastMediaSourceStreamMetadataChangedEventArgs(currentTrack, currentArtist));
        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
        public static event EventHandler CurrentStationChanged;
        public static event EventHandler BackgroundAudioError;
        public static event EventHandler<StationMediaPlayerConnectingStatusChangedEventArgs> ConnectingStatusChanged;
    }
}
