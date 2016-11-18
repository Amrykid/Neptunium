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
using Neptunium.Services.SnackBar;
using Crystal3.InversionOfControl;
using Microsoft.HockeyApp;
using Microsoft.HockeyApp.DataContracts;
using Windows.Media.Core;

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

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            IsInitialized = true;

            await Task.CompletedTask;
        }

        internal static void Pause()
        {
            if (CurrentStation != null && IsPlaying)
                BackgroundMediaPlayer.Current.Pause();
        }

        internal static void Play()
        {
            if (CurrentStation != null && !IsPlaying)
                BackgroundMediaPlayer.Current.Play();
        }

        private static void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {

        }

        public static MediaPlaybackSession PlaybackSession
        {
            get { return BackgroundMediaPlayer.Current?.PlaybackSession; }
        }

        public static void Deinitialize()
        {
            if (!IsInitialized) return;

            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

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

        public static double Volume
        {
            get { return BackgroundMediaPlayer.Current.Volume; }
            set { BackgroundMediaPlayer.Current.Volume = value; }
        }

        internal static void Stop()
        {
            if (BackgroundMediaPlayer.Current.PlaybackSession.CanPause)
                BackgroundMediaPlayer.Current.Pause();
        }

        public static async Task FadeVolumeDownToAsync(double value)
        {
            if (value >= StationMediaPlayer.Volume) throw new ArgumentOutOfRangeException(nameof(value));

            var initial = StationMediaPlayer.Volume;
            for (double x = initial; x > value; x -= .01)
            {
                await Task.Delay(25);
                StationMediaPlayer.Volume = x;
            }
        }
        public static async Task FadeVolumeUpToAsync(double value)
        {
            if (value <= StationMediaPlayer.Volume) throw new ArgumentOutOfRangeException(nameof(value));

            var initial = StationMediaPlayer.Volume;
            for (double x = initial; x < value; x += .01)
            {
                await Task.Delay(25);
                StationMediaPlayer.Volume = x;
            }
        }

        //public static ShoutcastStationInfo StationInfoFromStream { get { return currentStationMSSWrapper?.StationInfo; } }

        public static async Task<bool> PlayStationAsync(StationModel station)
        {
            if (station == currentStationModel && IsPlaying) return true;
            if (station == null) return false;

            await playStationResetEvent.WaitAsync();

            TracePlayStationAsyncCall(station);

            ShoutcastMediaSourceStream lastStream = null;

            if (IsPlaying)
            {
                if (currentStationMSSWrapper != null && (currentStationServerType == StationModelStreamServerType.Shoutcast || currentStationServerType == StationModelStreamServerType.Icecast))
                {
                    currentStationMSSWrapper.StationInfoChanged -= CurrentStationMSSWrapper_StationInfoChanged;
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

                    if (CurrentStationChanged != null) CurrentStationChanged(null, EventArgs.Empty);

                    if (ConnectingStatusChanged != null)
                        ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(false));
                }
                catch (Exception ex)
                {
                    playStationResetEvent.Release();

                    if (ConnectingStatusChanged != null)
                        ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(false));

                    HockeyClient.Current.TrackException(ex);

                    return false;
                }
            }
            else if ((currentStationServerType == StationModelStreamServerType.Shoutcast || currentStationServerType == StationModelStreamServerType.Radionomy))
            {
                currentStationMSSWrapper = new ShoutcastMediaSourceStream(new Uri(stream.Url), ConvertServerTypeToMediaServerType(currentStationServerType));

                currentStationMSSWrapper.MetadataChanged += CurrentStationMSSWrapper_MetadataChanged;
                currentStationMSSWrapper.StationInfoChanged += CurrentStationMSSWrapper_StationInfoChanged;


                try
                {
                    Action CleanUp = () =>
                    {
                        currentStationMSSWrapper.StationInfoChanged -= CurrentStationMSSWrapper_StationInfoChanged;
                        currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                        if (currentStationMSSWrapper.MediaStreamSource != null)
                            currentStationMSSWrapper.MediaStreamSource.Closed -= MediaStreamSource_Closed;
                    };

                    var connectTask = currentStationMSSWrapper.ConnectAsync(stream.SampleRate, stream.RelativePath);
                    var timeoutTask = Task.Delay(10000);
                    var resultTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (resultTask == connectTask)
                    {
                        if (await connectTask != null)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            BackgroundMediaPlayer.Current.SetMediaSource(currentStationMSSWrapper.MediaStreamSource);
#pragma warning restore CS0618 // Type or member is obsolete

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
                            CleanUp();
                        }
                    }
                    else
                    {
                        CleanUp();

                        //timeout.

                        if (BackgroundAudioError != null) BackgroundAudioError(null, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    currentStationMSSWrapper.StationInfoChanged -= CurrentStationMSSWrapper_StationInfoChanged;
                    currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                    if (currentStationMSSWrapper.MediaStreamSource != null)
                        currentStationMSSWrapper.MediaStreamSource.Closed -= MediaStreamSource_Closed;

                    HockeyClient.Current.TrackException(ex);

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

        private static void TracePlayStationAsyncCall(StationModel station)
        {
            EventTelemetry et = new EventTelemetry("PlayStationAsync");
            et.Properties.Add("Station", station.Name);
            HockeyClient.Current.TrackEvent(et);
        }

        private static async void CurrentStationMSSWrapper_StationInfoChanged(object sender, EventArgs e)
        {
            //show a message from the station if any. usually this only applies to radionomy stations.
            if (currentStationMSSWrapper != null)
            {
                if (currentStationMSSWrapper.StationInfo != null)
                {
                    var station = currentStationMSSWrapper.StationInfo;
                    if (!string.IsNullOrWhiteSpace(station.StationDescription))
                        await IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync(station.StationName + " - " + station.StationDescription, 6000);
                }
            }
        }

        private static ShoutcastServerType ConvertServerTypeToMediaServerType(StationModelStreamServerType currentStationServerType)
        {
            switch (currentStationServerType)
            {
                case StationModelStreamServerType.Shoutcast:
                    return ShoutcastServerType.Shoutcast;
                case StationModelStreamServerType.Radionomy:
                    return ShoutcastServerType.Radionomy;
                default:
                    return ShoutcastServerType.Shoutcast;
            }
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
                    break;
                default:
                    if (BackgroundAudioError != null) BackgroundAudioError(null, EventArgs.Empty);
                    break;
            }

            EventTelemetry et = new EventTelemetry("MediaStreamSource_Closed");
            et.Properties.Add("Reason", Enum.GetName(typeof(MediaStreamSourceClosedReason), args.Request.Reason));
            HockeyClient.Current.TrackEvent(et);
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

            EventTelemetry et = new EventTelemetry("UpdateNowPlaying");
            et.Properties.Add("CurrentTrack", currentTrack);
            et.Properties.Add("CurrentArtist", currentArtist);
            HockeyClient.Current.TrackEvent(et);
        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
        public static event EventHandler CurrentStationChanged;
        public static event EventHandler BackgroundAudioError;
        public static event EventHandler<StationMediaPlayerConnectingStatusChangedEventArgs> ConnectingStatusChanged;
    }
}
