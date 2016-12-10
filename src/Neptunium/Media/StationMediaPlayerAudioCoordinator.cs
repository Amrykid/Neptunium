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

namespace Neptunium.Media
{
    public class StationMediaPlayerAudioCoordinator : IDisposable
    {
        private ConnectionProfile internetConnectionProfile = null;
        private SystemMediaTransportControls smtc;
        private string currentTrack = "Title";
        private string currentArtist = "Artist";

        public IObservable<BasicSongInfo> MetadataReceived { get; private set; }
        private Subject<BasicSongInfo> metadataReceivedSub = null;

        public IObservable<StationMediaPlayerAudioCoordinationMessage> CoordinationMessageChannel { get; private set; }
        private Subject<StationMediaPlayerAudioCoordinationMessage> coordinationChannel = null;

        internal StationMediaPlayerAudioCoordinator()
        {
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

            metadataReceivedSub = new Subject<BasicSongInfo>();
            coordinationChannel = new Subject<StationMediaPlayerAudioCoordinationMessage>();

            CoordinationMessageChannel = coordinationChannel;
            MetadataReceived = metadataReceivedSub;

            BackgroundMediaPlayer.Current.MediaFailed += Current_MediaFailed;
        }

        public IMediaStreamer CurrentStreamer { get; private set; }
        public MediaPlaybackState PlaybackState { get; private set; }


        internal Task BeginStreamingAsync(IMediaStreamer streamer)
        {
            try
            {
                if (streamer.IsConnected)
                {
                    BackgroundMediaPlayer.Current.Source = streamer.Source;

                    BackgroundMediaPlayer.Current.Play();

                    CurrentStreamer = streamer;

                    internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();

                    streamer.MetadataChanged.Subscribe(songInfo =>
                    {
                        currentTrack = songInfo.Track;
                        currentArtist = songInfo.Artist;

                        metadataReceivedSub.OnNext(songInfo);

                        UpdateNowPlaying(songInfo.Track, songInfo.Artist);
                    });

                    streamer.ErrorOccurred.Subscribe(exception =>
                    {
                        coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationErrorMessage(StationMediaPlayerAudioCoordinationMessageType.AudioError, exception)
                        {
                            NetworkConnection = internetConnectionProfile
                        });
                    });

                    BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

                    return Task.CompletedTask;
                }

                return Task.FromException(new Exception("Not connected."));
            }
            catch (Exception ex)
            {
                BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

                return Task.FromException(ex);
            }
        }

        internal async Task StopStreamingCurrentStreamerAsync()
        {
            if (CurrentStreamer != null)
            {
                BackgroundMediaPlayer.Current.Pause();
                BackgroundMediaPlayer.Shutdown();

                await CurrentStreamer.DisconnectAsync();
                CurrentStreamer.Dispose();

                BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            }
        }

        private void UpdateNowPlaying(string currentTrack, string currentArtist)
        {
            try
            {
                smtc.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                smtc.DisplayUpdater.MusicProperties.Title = currentTrack;
                smtc.DisplayUpdater.MusicProperties.Artist = currentArtist;

                smtc.DisplayUpdater.Update();
            }
            catch (Exception) { }

            EventTelemetry et = new EventTelemetry("UpdateNowPlaying");
            et.Properties.Add("CurrentTrack", currentTrack);
            et.Properties.Add("CurrentArtist", currentArtist);
            HockeyClient.Current.TrackEvent(et);
        }

        #region Events

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
                                BackgroundMediaPlayer.Current.Source = CurrentStreamer.Source;
                                BackgroundMediaPlayer.Current.Play();

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

            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.None:
                    smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
                case MediaPlaybackState.Opening:
                    smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Paused:
                    smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlaybackState.Playing:
                    smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
            }

            coordinationChannel.OnNext(new StationMediaPlayerAudioCoordinationAudioPlaybackStatusMessage(sender.PlaybackState));
        }

        public void Dispose()
        {
            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
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
