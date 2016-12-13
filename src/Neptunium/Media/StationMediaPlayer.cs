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
using Windows.Networking.Connectivity;
using Neptunium.Media.Streamers;
using Crystal3;

namespace Neptunium.Media
{
    public static class StationMediaPlayer
    {
        private static StationMediaPlayerAudioCoordinator audioCoordinator = null;
        private static StationModelStreamServerType currentStationServerType;
        private static StationModelStream currentStream = null;

        private static StationModel currentStationModel = null;

        private static SemaphoreSlim playStationResetEvent = new SemaphoreSlim(1);


        public static bool IsInitialized { get; private set; }

        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

            audioCoordinator = new StationMediaPlayerAudioCoordinator();

            audioCoordinator.MetadataReceived.Subscribe(songInfo =>
            {
                if (MetadataChanged != null)
                    MetadataChanged(null, new ShoutcastMediaSourceStreamMetadataChangedEventArgs(songInfo.Track, songInfo.Artist));
            });

            audioCoordinator.CoordinationMessageChannel.Subscribe(message =>
            {
                switch (message.MessageType)
                {
                    case StationMediaPlayerAudioCoordinationMessageType.Reconnecting:
                        {
                            BackgroundAudioReconnecting?.Invoke(null, EventArgs.Empty);
                        }
                        break;
                    case StationMediaPlayerAudioCoordinationMessageType.PlaybackState:
                        {
                            var playMsg = message as StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage;

                            if (playMsg != null)
                            {
                                IsPlaying = playMsg.PlaybackState == MediaPlaybackState.Playing || playMsg.PlaybackState == MediaPlaybackState.Buffering;
                            }
                        }
                        break;
                    default:
                        {
                            var errorMsg = message as StationMediaPlayerAudioCoordinationErrorMessage;

                            BackgroundAudioError?.Invoke(null, new StationMediaPlayerBackgroundAudioErrorEventArgs()
                            {
                                Exception = errorMsg.Exception,
                                NetworkConnectionProfile = errorMsg.NetworkConnection,
                                Station = audioCoordinator.CurrentStreamer?.CurrentStation
                            });
                        }
                        break;
                }

            });

            IsInitialized = true;

            await Task.CompletedTask;
        }

        internal static void Pause()
        {
            if (CurrentStation != null && IsPlaying)
                audioCoordinator.CurrentStreamer.Player.Pause();
        }

        internal static void Play()
        {
            if (CurrentStation != null && !IsPlaying)
                audioCoordinator.CurrentStreamer.Player.Play();
        }

        public static void Deinitialize()
        {
            if (!IsInitialized) return;

            audioCoordinator.Dispose();

            IsInitialized = false;
        }

        public static StationModel CurrentStation { get { return currentStationModel; } }

        private static bool _isPlaying = false;
        public static bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            private set
            {
                _isPlaying = value;

                IsPlayingChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static double Volume
        {
            get { return (double)audioCoordinator.CurrentStreamer?.Player.Volume; }
            set { if (audioCoordinator.CurrentStreamer != null) { audioCoordinator.CurrentStreamer.Player.Volume = value; } }
        }

        internal static void Stop()
        {
            Pause();
        }

        public static async Task FadeVolumeDownToAsync(double value)
        {
            if (audioCoordinator.CurrentStreamer is BasicMediaStreamer)
            {
                await ((BasicMediaStreamer)audioCoordinator.CurrentStreamer).FadeVolumeDownToAsync(value);
            }
        }
        public static async Task FadeVolumeUpToAsync(double value)
        {
            if (audioCoordinator.CurrentStreamer is BasicMediaStreamer)
            {
                await ((BasicMediaStreamer)audioCoordinator.CurrentStreamer).FadeVolumeUpToAsync(value);
            }
        }

        public static async Task<bool> PlayStationAsync(StationModel station)
        {
            if (!IsInitialized)
                await InitializeAsync();

            if (station == currentStationModel && IsPlaying) return true;
            if (station == null) return false;

            await playStationResetEvent.WaitAsync();

            TracePlayStationAsyncCall(station);

            if (ConnectingStatusChanged != null)
                ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(true));


            var stream = station.Streams.First();

            var streamer = StreamerFactory.CreateStreamerFromServerType(stream.ServerType);

            await streamer.ConnectAsync(station, stream, null);

            bool willCrossFade = false; //todo make cross fade transitions a setting

            switch(CrystalApplication.GetDevicePlatform())
            {
                case Crystal3.Core.Platform.Xbox:
                case Crystal3.Core.Platform.Desktop:
                    willCrossFade = audioCoordinator.CurrentStreamer != null;
                    break;
                default: //cross fade transitioning seems to studder on mobile. might be because of the SD400
                    willCrossFade = false;
                    break;

            }          

            if (streamer.IsConnected)
            {
                currentStationModel = station;

                currentStream = stream;

                currentStationServerType = stream.ServerType;

                if (CurrentStationChanged != null) CurrentStationChanged(null, EventArgs.Empty);

                IsPlaying = true;

                if (!willCrossFade)
                {
                    await audioCoordinator.StopStreamingCurrentStreamerAsync();

                    await audioCoordinator.BeginStreamingAsync(streamer);
                }
                else
                {
                    await Task.Delay(3000); //wait to buffer

                    await audioCoordinator.BeginStreamingTransitionAsync(streamer);
                }

                //should be playing at this point.

                IsPlaying = true;

                if (ConnectingStatusChanged != null)
                    ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(false));
            }
            else
            {
                if (ConnectingStatusChanged != null)
                    ConnectingStatusChanged(null, new StationMediaPlayerConnectingStatusChangedEventArgs(false));

                //connection error

                BackgroundAudioError?.Invoke(null, new StationMediaPlayerBackgroundAudioErrorEventArgs()
                {
                    Exception = new Exception("Unable to connect."),
                    Station = station,
                    StillPlaying = audioCoordinator.CurrentStreamer != null ? audioCoordinator.CurrentStreamer.IsConnected : false
                });

                if (audioCoordinator.CurrentStreamer == null || !(bool)audioCoordinator.CurrentStreamer?.IsConnected)
                {
                    IsPlaying = false;

                    currentStationModel = null;

                    currentStream = null;

                    currentStationServerType = stream.ServerType;
                }
            }

            playStationResetEvent.Release();

            return IsPlaying;
        }

        private static void TracePlayStationAsyncCall(StationModel station)
        {

#if !DEBUG
            EventTelemetry et = new EventTelemetry("PlayStationAsync");
            et.Properties.Add("Station", station.Name);
            HockeyClient.Current.TrackEvent(et);
#endif
        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
        public static event EventHandler CurrentStationChanged;
        public static event EventHandler BackgroundAudioReconnecting;
        public static event EventHandler<StationMediaPlayerBackgroundAudioErrorEventArgs> BackgroundAudioError;
        public static event EventHandler<StationMediaPlayerConnectingStatusChangedEventArgs> ConnectingStatusChanged;
        public static event EventHandler IsPlayingChanged;
    }
}
