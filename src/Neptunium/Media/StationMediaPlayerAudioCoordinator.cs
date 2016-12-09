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
    public class StationMediaPlayerAudioCoordinator: IDisposable
    {
        private ConnectionProfile internetConnectionProfile = null;
        private SystemMediaTransportControls smtc;
        private string currentTrack = "Title";
        private string currentArtist = "Artist";

        public IObservable<BasicSongInfo> MetadataReceived { get; private set; }
        private Subject<BasicSongInfo> metadataReceivedSub = null;

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

            MetadataReceived = metadataReceivedSub;

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        }

        public IMediaStreamer CurrentStreamer { get; private set; }

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


                    UpdateNowPlaying(currentTrack, currentArtist);

                    return Task.CompletedTask;
                }

                return Task.FromException(new Exception("Not connected."));
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        internal async Task StopStreamingCurrentStreamerAsync()
        {
            if (CurrentStreamer != null)
            {
                await CurrentStreamer.DisconnectAsync();
                CurrentStreamer.Dispose();
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

        private static void MediaStreamSource_Closed(Windows.Media.Core.MediaStreamSource sender, Windows.Media.Core.MediaStreamSourceClosedEventArgs args)
        {

            EventTelemetry et = new EventTelemetry("MediaStreamSource_Closed");
            et.Properties.Add("Reason", Enum.GetName(typeof(MediaStreamSourceClosedReason), args.Request.Reason));
            HockeyClient.Current.TrackEvent(et);
        }


        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
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
        }

        public void Dispose()
        {
            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;

            BackgroundMediaPlayer.Current.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
        }
        #endregion
    }
}
