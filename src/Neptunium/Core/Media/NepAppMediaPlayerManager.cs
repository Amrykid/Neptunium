using Neptunium.Core;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Media.Playback;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager : INepAppFunctionManager, INotifyPropertyChanged
    {
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
                    return new ShoutcastStationMediaStreamer();
            }

            return null;
        }

        public async Task TryStreamStationAsync(StationStream stream)
        {
            if (!NepApp.Network.IsConnected) throw new NeptuniumNetworkConnectionRequiredException();

            BasicNepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);
            await streamer.TryConnectAsync(stream); //a failure to connect is caught as a Neptunium.Core.NeptuniumStreamConnectionFailedException by AppShellViewModel

            if (CurrentStreamer != null)
            {
                CurrentStreamer.MetadataChanged -= Streamer_MetadataChanged;

                CurrentStreamer.Pause();
                CurrentStreamer.Dispose();
            }

            if (CurrentPlayer != null)
            {
                CurrentPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                CurrentPlayer.Dispose();
            }

            CurrentPlayer = new MediaPlayer();
            CurrentPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
            CurrentPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            streamer.InitializePlayback(CurrentPlayer);

            UpdateMetadata(streamer.SongMetadata);

            streamer.MetadataChanged += Streamer_MetadataChanged;

            streamer.Play();

            CurrentStreamer = streamer;
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.Buffering:
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Paused:
                case MediaPlaybackState.None:
                    //show play
                    IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(false));
                    break;
                case MediaPlaybackState.Playing:
                    //show pause
                    IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(true));
                    break;
            }
        }

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
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                CurrentMetadata = metadata;
                RaisePropertyChanged(nameof(CurrentMetadata));
            });
        }
    }
}