using Crystal3;
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

        public IMediaStreamer CurrentStreamer { get; private set; }
        public MediaPlaybackState PlaybackState { get; private set; }


        internal async Task BeginStreamingAsync(BasicMediaStreamer streamer)
        {
            try
            {
                if (streamer.IsConnected)
                {
                    streamer.Player.Play();
                    await streamer.FadeVolumeUpToAsync(1.0);
                    UseStreamerBase(streamer);
                    

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
                if (streamer.IsConnected)
                {
                    if (CurrentStreamer.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused || CurrentStreamer.Player.IsMuted)
                    {
                        //no point fading if it can't be heard anyway.

                        await StopStreamingCurrentStreamerAsync();
                        await BeginStreamingAsync(streamer);
                        return;
                    }

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

            systemMediaTransportControls = streamer.Player.SystemMediaTransportControls;

            streamer.MetadataChanged.Subscribe(songInfo =>
            {
                currentTrack = songInfo.Track;
                currentArtist = songInfo.Artist;

                UpdateNowPlaying(streamer, songInfo);
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

            UpdateNowPlaying(streamer, new BasicSongInfo() { Track = streamer.CurrentTrack, Artist = streamer.CurrentArtist });
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

        private void UpdateNowPlaying(IMediaStreamer streamer, BasicSongInfo songInfo)
        {
            if (songInfo == null) return;

            if (streamer != null)
            {
                if (streamer.CurrentStation != null)
                    if ((bool)streamer.CurrentStation.StationMessages?.Contains(songInfo.Track)) return; //don't play that pre-defined station message that happens every so often.

                if (systemMediaTransportControls != null)
                {
                    systemMediaTransportControls.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                    systemMediaTransportControls.DisplayUpdater.MusicProperties.Title = songInfo.Track;
                    systemMediaTransportControls.DisplayUpdater.MusicProperties.Artist = songInfo.Artist;

                    if (streamer.CurrentStation != null)
                        if (!string.IsNullOrWhiteSpace(streamer.CurrentStation.Logo))
                            systemMediaTransportControls.DisplayUpdater.Thumbnail =
                                RandomAccessStreamReference.CreateFromUri(new Uri(streamer.CurrentStation.Logo));

                    systemMediaTransportControls.DisplayUpdater.Update();
                }

                metadataReceivedSub.OnNext(songInfo);

                EventTelemetry et = new EventTelemetry("UpdateNowPlaying");
                et.Properties.Add("CurrentTrack", currentTrack);
                et.Properties.Add("CurrentArtist", currentArtist);
                HockeyClient.Current.TrackEvent(et);
            }
        }

        #region Events
        private async void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            //EventTelemetry et = new EventTelemetry("MediaStreamSource_Closed");
            //et.Properties.Add("Reason", Enum.GetName(typeof(MediaStreamSourceClosedReason), args.Request.Reason));
            //HockeyClient.Current.TrackEvent(et);

            Func<bool> shouldReconnect = () =>
            {
                if (args.Error == MediaPlayerError.NetworkError)
                    return true;

                if (args.Error == MediaPlayerError.DecodingError)
                {
                    switch (args.ExtendedErrorCode.HResult)
                    {
                        case -1072872829:
                        case -1072846852:
                            return true;
                        default:
                            return false;
                    }
                }

                if (args.ErrorMessage.Contains("0xC00D4283"))
                    return true;

                return false;
            };

            if (shouldReconnect())
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
                                internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
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

            //systemMediaTransportControls.PlaybackStatus = ConvertToSystemMediaPlaybackStatus(sender.PlaybackState);

            coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage(sender.PlaybackState));
        }

        private MediaPlaybackStatus ConvertToSystemMediaPlaybackStatus(MediaPlaybackState playbackState)
        {
            switch (playbackState)
            {
                case MediaPlaybackState.Opening:
                    return MediaPlaybackStatus.Changing;
                case MediaPlaybackState.Paused:
                    return MediaPlaybackStatus.Paused;
                case MediaPlaybackState.Buffering:
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

            metadataReceivedSub.Dispose();
            coordinationChannel.Dispose();

            GC.SuppressFinalize(this);
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
