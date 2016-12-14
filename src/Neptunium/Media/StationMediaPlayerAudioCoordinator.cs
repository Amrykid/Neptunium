using Microsoft.HockeyApp;
using Microsoft.HockeyApp.DataContracts;
using Neptunium.Media.Streamers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;

namespace Neptunium.Media
{
    public class StationMediaPlayerAudioCoordinator : IDisposable
    {
        private ConnectionProfile internetConnectionProfile = null;
        private string currentTrack = "Title";
        private string currentArtist = "Artist";

        public IObservable<BasicSongInfo> MetadataReceived { get; private set; }
        private Subject<BasicSongInfo> metadataReceivedSub = null;

        public IObservable<StationMediaPlayerAudioCoordinationMessage> CoordinationMessageChannel { get; private set; }
        private Subject<StationMediaPlayerAudioCoordinationMessage> coordinationChannel = null;

        private SystemMediaTransportControls systemMediaTransportControls = null;

        internal StationMediaPlayerAudioCoordinator()
        {
            metadataReceivedSub = new Subject<BasicSongInfo>();
            coordinationChannel = new Subject<StationMediaPlayerAudioCoordinationMessage>();

            CoordinationMessageChannel = coordinationChannel;
            MetadataReceived = metadataReceivedSub;
        }

        private void AcquireMediaControlsIfNeeded()
        {
            if (systemMediaTransportControls == null)
            {
                systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
                systemMediaTransportControls.IsChannelDownEnabled = false;
                systemMediaTransportControls.IsChannelUpEnabled = false;
                systemMediaTransportControls.IsEnabled = true;
                systemMediaTransportControls.IsFastForwardEnabled = false;
                systemMediaTransportControls.IsNextEnabled = false;
                systemMediaTransportControls.IsPauseEnabled = true;
                systemMediaTransportControls.IsPlayEnabled = true;
                systemMediaTransportControls.IsPreviousEnabled = false;
                systemMediaTransportControls.IsRecordEnabled = false;
                systemMediaTransportControls.IsRewindEnabled = false;
                systemMediaTransportControls.IsStopEnabled = false;

                systemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;
            }
        }

        private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (CurrentStreamer == null) return;

            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    if (CurrentStreamer.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
                        CurrentStreamer.Player.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    if (CurrentStreamer.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                        CurrentStreamer.Player.Pause();
                    break;

            }
        }

        public IMediaStreamer CurrentStreamer { get; private set; }
        public MediaPlaybackState PlaybackState { get; private set; }


        internal async Task BeginStreamingAsync(BasicMediaStreamer streamer)
        {
            try
            {
                AcquireMediaControlsIfNeeded();

                if (streamer.IsConnected)
                {
                    streamer.Player.Play();
                    UseStreamerBase(streamer);
                    await streamer.FadeVolumeUpToAsync(1.0);

                    CurrentStreamer = streamer;
                }
                else
                    throw new Exception("Not connected.");
            }
            catch (Exception ex)
            {
                streamer.Player.MediaFailed -= Current_MediaFailed;
                streamer.Player.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

                throw new Exception("Inner exception", ex);
            }
        }

        internal async Task BeginStreamingTransitionAsync(BasicMediaStreamer streamer)
        {
            if (CurrentStreamer == null)
            {
                throw new Exception("Not streaming anything currently.");
            }

            try
            {
                AcquireMediaControlsIfNeeded();

                if (streamer.IsConnected)
                {
                    streamer.Player.Play();
                    UseStreamerBase(streamer);
                    await Task.WhenAll(streamer.FadeVolumeUpToAsync(1.0), ((BasicMediaStreamer)CurrentStreamer).FadeVolumeDownToAsync(0.0));
                    await StopStreamingCurrentStreamerAsync();

                    CurrentStreamer = streamer;
                }
                else
                    throw new Exception("Not connected.");
            }
            catch (Exception ex)
            {
                streamer.Player.MediaFailed -= Current_MediaFailed;
                streamer.Player.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

                throw new Exception("Inner exception", ex);
            }
        }

        private void UseStreamerBase(IMediaStreamer streamer)
        {
            internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

            streamer.MetadataChanged.Subscribe(songInfo =>
            {
                currentTrack = songInfo.Track;
                currentArtist = songInfo.Artist;

                UpdateNowPlaying(songInfo);
            });

            streamer.ErrorOccurred.Subscribe(exception =>
            {
                coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType.AudioError, exception)
                {
                    NetworkConnection = internetConnectionProfile
                });
            });

            streamer.Player.MediaFailed += Current_MediaFailed;

            streamer.Player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            UpdateNowPlaying(new BasicSongInfo() { Track = streamer.CurrentTrack, Artist = streamer.CurrentArtist });
        }

        internal async Task StopStreamingCurrentStreamerAsync()
        {
            if (CurrentStreamer != null)
            {
                CurrentStreamer.Player.Pause();

                await CurrentStreamer.DisconnectAsync();

                CurrentStreamer.Player.MediaFailed -= Current_MediaFailed;
                CurrentStreamer.Player.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

                CurrentStreamer.Dispose();
            }
        }

        private void UpdateNowPlaying(BasicSongInfo songInfo)
        {
            if ((bool)CurrentStreamer.CurrentStation.StationMessages?.Contains(songInfo.Track)) return; //don't play that pre-defined station message that happens every so often.

            if (systemMediaTransportControls != null)
            {
                try
                {

                    systemMediaTransportControls.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                    systemMediaTransportControls.DisplayUpdater.MusicProperties.Title = songInfo.Track;
                    systemMediaTransportControls.DisplayUpdater.MusicProperties.Artist = songInfo.Artist;

                    if (!string.IsNullOrWhiteSpace(CurrentStreamer.CurrentStation.Logo))
                        systemMediaTransportControls.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CurrentStreamer.CurrentStation.Logo));

                    systemMediaTransportControls.DisplayUpdater.Update();
                }
                catch (Exception) { }
            }

            metadataReceivedSub.OnNext(songInfo);

            EventTelemetry et = new EventTelemetry("UpdateNowPlaying");
            et.Properties.Add("CurrentTrack", currentTrack);
            et.Properties.Add("CurrentArtist", currentArtist);
            HockeyClient.Current.TrackEvent(et);
        }

        #region Events
        private async void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            //EventTelemetry et = new EventTelemetry("MediaStreamSource_Closed");
            //et.Properties.Add("Reason", Enum.GetName(typeof(MediaStreamSourceClosedReason), args.Request.Reason));
            //HockeyClient.Current.TrackEvent(et);

            bool networkError = args.Error == MediaPlayerError.NetworkError || (args.Error == MediaPlayerError.DecodingError && args.ExtendedErrorCode.HResult == -1072872829);

            if (networkError)
            {
                await Task.Delay(500); //give the system time to figure out the new network connection
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();

                if (connectionProfile != null && internetConnectionProfile != null)
                {
                    if (connectionProfile.ProfileName != internetConnectionProfile.ProfileName)
                    {
                        //reconnect

                        if (CurrentStreamer != null)
                        {
                            coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationMessage(StationMediaPlayerAudioCoordinationMessageType.Reconnecting));

                            await CurrentStreamer.ReconnectAsync();

                            if (CurrentStreamer.IsConnected)
                            {
                                CurrentStreamer.Player.Play();

                                return;
                            }
                            else
                            {
                                coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType.ReconnectionFailed, new Exception()));

                                return;
                            }
                        }
                    }
                }
                else if (connectionProfile == null && internetConnectionProfile != null)
                {
                    //we lost our connection in this case.
                    coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType.NetworkConnectionLost, new Exception("We've lost our network connection!")));

                    return;
                }
            }

            //otherwise, audio error

            coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType.AudioError, args.ExtendedErrorCode)
            {
                PlayerError = args.Error,
                NetworkConnection = internetConnectionProfile
            });
        }


        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            PlaybackState = sender.PlaybackState;

            systemMediaTransportControls.PlaybackStatus = ConvertToSystemMediaPlaybackStatus(sender.PlaybackState);

            coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage(sender.PlaybackState));
        }

        private MediaPlaybackStatus ConvertToSystemMediaPlaybackStatus(MediaPlaybackState playbackState)
        {
            switch (playbackState)
            {
                case MediaPlaybackState.Buffering:
                    return MediaPlaybackStatus.Changing;
                case MediaPlaybackState.Opening:
                    return MediaPlaybackStatus.Changing;
                case MediaPlaybackState.Paused:
                    return MediaPlaybackStatus.Paused;
                case MediaPlaybackState.Playing:
                    return MediaPlaybackStatus.Playing;
                case MediaPlaybackState.None:
                default:
                    return MediaPlaybackStatus.Closed;
            }
        }

        public void Dispose()
        {
            if (CurrentStreamer != null)
            {
                CurrentStreamer.Player.MediaFailed -= Current_MediaFailed;
                CurrentStreamer.Player.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            }

            if (systemMediaTransportControls != null)
            {
                systemMediaTransportControls.ButtonPressed -= SystemMediaTransportControls_ButtonPressed;
            }
        }
        #endregion
    }

    public class StationMediaPlayerAudioCoordinationMessage
    {
        public StationMediaPlayerAudioCoordinationMessageType MessageType { get; private set; }

        public StationMediaPlayerAudioCoordinationMessage(StationMediaPlayerAudioCoordinationMessageType type)
        {
            MessageType = type;
        }
    }

    public class StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage : StationMediaPlayerAudioCoordinationMessage
    {
        public MediaPlaybackState PlaybackState { get; private set; }
        public StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage(MediaPlaybackState state) : base(StationMediaPlayerAudioCoordinationMessageType.PlaybackState)
        {
            PlaybackState = state;
        }
    }

    public class StationMediaPlayerAudioCoordinationErrorMessage : StationMediaPlayerAudioCoordinationMessage
    {
        public Exception Exception { get; private set; }

        public ConnectionProfile NetworkConnection { get; set; }

        public MediaSourceError MediaError { get; set; }

        public MediaPlayerError PlayerError { get; set; }

        public StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType type, Exception ex) : base(type)
        {
            Exception = ex;
        }
    }

    public enum StationMediaPlayerAudioCoordinationMessageType
    {
        AudioError,
        Reconnecting,
        ReconnectionFailed,
        NetworkConnectionLost,
        PlaybackState
    }
}
