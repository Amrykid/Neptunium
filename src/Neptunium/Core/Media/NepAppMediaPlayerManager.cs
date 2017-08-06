using Neptunium.Core;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        private SystemMediaTransportControls systemMediaTransportControls = null;

        internal NepAppMediaPlayerManager()
        {

        }

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        public SongMetadata CurrentMetadata { get; private set; }

        internal void Pause()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            CurrentPlayer.Pause();
        }

        internal void Resume()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            CurrentPlayer.Play();
        }

        private MediaPlayer CurrentPlayer { get; set; }
        private MediaPlaybackSession CurrentPlayerSession { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BasicNepAppMediaStreamer CreateStreamerForServerFormat(StationStreamServerFormat format)
        {
            switch (format)
            {
                case StationStreamServerFormat.Direct:
                    return new DirectStationMediaStreamer();
                case StationStreamServerFormat.Shoutcast:
                case StationStreamServerFormat.Icecast:
                case StationStreamServerFormat.Radionomy:
                    return new ShoutcastStationMediaStreamer();
            }

            return null;
        }

        public async Task TryStreamStationAsync(StationStream stream)
        {
            if (!NepApp.Network.IsConnected) throw new NeptuniumNetworkConnectionRequiredException();

            BasicNepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);

            Task timeoutTask = null;
            Task connectionTask = null;


            timeoutTask = Task.Delay(10000);
            connectionTask = streamer.TryConnectAsync(stream); //a failure to connect is caught as a Neptunium.Core.NeptuniumStreamConnectionFailedException by AppShellViewModel

            Task result = await Task.WhenAny(connectionTask, timeoutTask);
            if (result == timeoutTask)
            {
                throw new NeptuniumStreamConnectionFailedException(stream, message: "Connection to server timed out.");
            }

            if (CurrentStreamer != null)
            {
                CurrentStreamer.MetadataChanged -= Streamer_MetadataChanged;

                CurrentStreamer.Pause();
                CurrentStreamer.Dispose();
            }

            if (CurrentPlayer != null)
            {
                CurrentPlayerSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                CurrentPlayer.Dispose();
            }

            CurrentPlayer = new MediaPlayer();

            systemMediaTransportControls = CurrentPlayer.SystemMediaTransportControls;
            systemMediaTransportControls.IsChannelDownEnabled = false;
            systemMediaTransportControls.IsChannelUpEnabled = false;
            systemMediaTransportControls.IsFastForwardEnabled = false;
            systemMediaTransportControls.IsNextEnabled = false;
            systemMediaTransportControls.IsPreviousEnabled = false;
            systemMediaTransportControls.IsRewindEnabled = false;
            systemMediaTransportControls.IsRewindEnabled = false;
            systemMediaTransportControls.IsPlayEnabled = true;
            systemMediaTransportControls.IsPauseEnabled = true;

            CurrentPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
            CurrentPlayer.CommandManager.IsEnabled = true;
            CurrentPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
            CurrentPlayerSession = CurrentPlayer.PlaybackSession;
            CurrentPlayerSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            streamer.InitializePlayback(CurrentPlayer);

            await CurrentPlayer.WaitForMediaOpenAsync();

            streamer.MetadataChanged += Streamer_MetadataChanged;

            streamer.Play();

            UpdateMetadata(streamer.SongMetadata);

            CurrentStreamer = streamer;
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.Buffering:
                case MediaPlaybackState.Opening:
                    //show play
                    IsPlaying = false;
                    IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(false));
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Paused:
                case MediaPlaybackState.None:
                    //show play
                    IsPlaying = false;
                    IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(false));
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlaybackState.Playing:
                    //show pause
                    IsPlaying = true;
                    IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(true));
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
            }
        }

        public bool IsPlaying { get; private set; }
        public event EventHandler<NepAppMediaPlayerManagerIsPlayingEventArgs> IsPlayingChanged;

        public class NepAppMediaPlayerManagerIsPlayingEventArgs : EventArgs
        {
            internal NepAppMediaPlayerManagerIsPlayingEventArgs(bool isPlaying) { IsPlaying = isPlaying; }
            public bool IsPlaying { get; private set; }
        }

        private void Streamer_MetadataChanged(object sender, MediaStreamerMetadataChangedEventArgs e)
        {
            UpdateMetadata(e.Metadata);
        }

        private void UpdateMetadata(SongMetadata metadata)
        {
            if (metadata == null) return;

            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                CurrentMetadata = metadata;

                try
                {
                    var updater = systemMediaTransportControls.DisplayUpdater;
                    updater.Type = MediaPlaybackType.Music;
                    updater.MusicProperties.Title = metadata.Track;
                    updater.MusicProperties.Artist = metadata.Artist;
                    updater.AppMediaId = metadata.StationPlayedOn.Name.GetHashCode().ToString();
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(metadata.StationPlayedOn.StationLogoUrl);
                    updater.Update();
                }
                catch (COMException) { }

                RaisePropertyChanged(nameof(CurrentMetadata));
            });
        }
    }
}