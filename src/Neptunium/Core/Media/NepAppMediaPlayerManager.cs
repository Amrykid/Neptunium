using Neptunium.Core;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
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
        private SemaphoreSlim playLock = null;

        internal NepAppMediaPlayerManager()
        {
            playLock = new SemaphoreSlim(1);
        }

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        public SongMetadata CurrentMetadata { get; private set; }
        internal StationStream CurrentStream { get; private set; }

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

            await playLock.WaitAsync();

            ConnectingBegin?.Invoke(this, EventArgs.Empty);

            BasicNepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);

            Task timeoutTask = null;
            Task connectionTask = null;


            timeoutTask = Task.Delay(10000);
            connectionTask = streamer.TryConnectAsync(stream); //a failure to connect is caught as a Neptunium.Core.NeptuniumStreamConnectionFailedException by AppShellViewModel

            Task result = await Task.WhenAny(connectionTask, timeoutTask);
            if (result == timeoutTask || connectionTask.IsFaulted)
            {
                ConnectingEnd?.Invoke(this, EventArgs.Empty);
                playLock.Release();
                throw new NeptuniumStreamConnectionFailedException(stream, message: "Connection to server timed out.");
            }

            ShutdownPreviousPlaybackSession();

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
            CurrentPlayer.MediaFailed += CurrentPlayer_MediaFailed;
            CurrentPlayerSession = CurrentPlayer.PlaybackSession;
            CurrentPlayerSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            streamer.InitializePlayback(CurrentPlayer);

            await CurrentPlayer.WaitForMediaOpenAsync();

            streamer.MetadataChanged += Streamer_MetadataChanged;

            streamer.Play();

            ConnectingEnd?.Invoke(this, EventArgs.Empty);

            UpdateMetadata(streamer.SongMetadata);

            CurrentStreamer = streamer;
            CurrentStream = stream;

            playLock.Release();
        }

        private void ShutdownPreviousPlaybackSession()
        {
            if (CurrentStreamer != null)
            {
                CurrentStreamer.MetadataChanged -= Streamer_MetadataChanged;

                CurrentStreamer.Pause();
                CurrentStreamer.Dispose();
                CurrentStreamer = null;
            }

            if (CurrentPlayer != null)
            {
                CurrentPlayer.MediaFailed -= CurrentPlayer_MediaFailed;
                CurrentPlayerSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                CurrentPlayer.Dispose();
                CurrentPlayer = null;
            }

            CurrentStream = null;
            CurrentMetadata = null;

            App.Dispatcher.RunAsync(() =>
            {
                RaisePropertyChanged(nameof(CurrentMetadata));
                IsPlaying = false;
                RaisePropertyChanged(nameof(IsPlaying));
            });
        }

        private async void CurrentPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            var stream = CurrentStream;
            ShutdownPreviousPlaybackSession();
            if (!NepApp.Network.IsConnected)
            {
                await NepApp.UI.ShowErrorDialogAsync("Uh-Oh!", "Network connection lost!");
            }
            else
            {
                await NepApp.UI.ShowErrorDialogAsync("Uh-Oh!", "An unknown error occurred.");
            }
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

        public event EventHandler ConnectingBegin;
        public event EventHandler ConnectingEnd;

        public bool IsPlaying { get; private set; }
        public event EventHandler<NepAppMediaPlayerManagerIsPlayingEventArgs> IsPlayingChanged;

        public class NepAppMediaPlayerManagerIsPlayingEventArgs : EventArgs
        {
            internal NepAppMediaPlayerManagerIsPlayingEventArgs(bool isPlaying) { IsPlaying = isPlaying; }
            public bool IsPlaying { get; private set; }
        }

        private async void Streamer_MetadataChanged(object sender, MediaStreamerMetadataChangedEventArgs e)
        {
            UpdateMetadata(e.Metadata);

            //todo get extended metadata info.

            if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
            {
                NepApp.UI.ToastNotifier.ShowSongToastNotification(e.Metadata);
                //todo update tile with now playing info
            }
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