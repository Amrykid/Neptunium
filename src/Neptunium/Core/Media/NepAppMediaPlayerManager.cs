using Neptunium.Core;
using Neptunium.Core.Media.Bluetooth;
using Neptunium.Core.Media.History;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Casting;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager : INepAppFunctionManager, INotifyPropertyChanged
    {
        private SemaphoreSlim playLock = null;
        private DispatcherTimer sleepTimer = new DispatcherTimer();

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        internal StationStream CurrentStream { get; private set; }
        public NepAppMediaBluetoothManager Bluetooth { get; private set; }
        private MediaPlayer CurrentPlayer { get; set; }
        public SystemMediaTransportControls MediaTransportControls { get; private set; }
        private MediaPlaybackSession CurrentPlayerSession { get; set; }
        public CastingConnection MediaCastingConnection { get; private set; }
        internal bool IsSleepTimerRunning { get { return sleepTimer.IsEnabled; } }
        public event PropertyChangedEventHandler PropertyChanged;
        public double Volume
        {
            get { return (double)CurrentPlayer?.Volume; }
        }
        public bool IsPlaying { get; private set; }
        public bool IsCasting { get; private set; }

        public event EventHandler ConnectingBegin;
        public event EventHandler ConnectingEnd;
        public event EventHandler<NepAppMediaPlayerManagerIsPlayingEventArgs> IsPlayingChanged;
        public event EventHandler<EventArgs> IsCastingChanged;

        public class NepAppMediaPlayerManagerIsPlayingEventArgs : EventArgs
        {
            internal NepAppMediaPlayerManagerIsPlayingEventArgs(bool isPlaying) { IsPlaying = isPlaying; }
            public bool IsPlaying { get; private set; }
        }


        internal NepAppMediaPlayerManager()
        {
            playLock = new SemaphoreSlim(1);
            Bluetooth = new NepAppMediaBluetoothManager(this);

            sleepTimer.Tick += SleepTimer_Tick;
        }

        internal void Pause()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            if (!(bool)CurrentPlayer.PlaybackSession?.CanPause) return;
            CurrentPlayer.Pause();
        }

        internal void Resume()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            if (CurrentPlayer.PlaybackSession?.PlaybackState != MediaPlaybackState.Paused) return;
            CurrentPlayer.Play();
        }

        private void RaisePropertyChanged(string propertyName)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
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

        internal void SetSleepTimer(TimeSpan timeToWait)
        {
            if (sleepTimer.IsEnabled)
            {
                sleepTimer.Stop();
            }

            sleepTimer.Interval = timeToWait;

            sleepTimer.Start();
        }

        internal void ClearSleepTimer()
        {
            if (sleepTimer.IsEnabled) sleepTimer.Stop();
        }

        private async void SleepTimer_Tick(object sender, object e)
        {
            if (IsPlaying)
            {
                Pause();

                if (!await App.GetIfPrimaryWindowVisibleAsync())
                {
                    NepApp.UI.Notifier.ShowGenericToastNotification("Sleep Timer", "Media paused.", "sleep-timer");
                }
            }

            sleepTimer.Stop();
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
                streamer.Dispose();
                playLock.Release();
                throw new NeptuniumStreamConnectionFailedException(stream, connectionTask.Exception?.InnerException?.Message ?? "Connection timed out.");
            }

            ShutdownPreviousPlaybackSession();

            CurrentPlayer = new MediaPlayer();

            MediaTransportControls = CurrentPlayer.SystemMediaTransportControls;
            MediaTransportControls.IsChannelDownEnabled = false;
            MediaTransportControls.IsChannelUpEnabled = false;
            MediaTransportControls.IsFastForwardEnabled = false;
            MediaTransportControls.IsNextEnabled = false;
            MediaTransportControls.IsPreviousEnabled = false;
            MediaTransportControls.IsRewindEnabled = false;
            MediaTransportControls.IsRewindEnabled = false;
            MediaTransportControls.IsPlayEnabled = true;
            MediaTransportControls.IsPauseEnabled = true;

            CurrentPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
            CurrentPlayer.CommandManager.IsEnabled = true;
            CurrentPlayer.AudioDeviceType = MediaPlayerAudioDeviceType.Multimedia;
            CurrentPlayer.MediaFailed += CurrentPlayer_MediaFailed;
            CurrentPlayerSession = CurrentPlayer.PlaybackSession;
            CurrentPlayerSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            streamer.InitializePlayback(CurrentPlayer);

            await CurrentPlayer.WaitForMediaOpenAsync();

            streamer.MetadataChanged += Streamer_MetadataChanged;

            CurrentStreamer = streamer;
            CurrentStream = stream;

            streamer.Play();

            NepApp.Stations.SetLastPlayedStationName(stream.ParentStation.Name);

            if (streamer.SongMetadata == null)
            {
                NepApp.SongManager.SetCurrentMetadataToUnknown();
            }
            else
            {
                NepApp.SongManager.HandleMetadata(streamer.SongMetadata, stream);
            }

            ConnectingEnd?.Invoke(this, EventArgs.Empty);


            playLock.Release();
        }

        private void ShutdownPreviousPlaybackSession()
        {
            if (CurrentStreamer == null && CurrentPlayer == null) return;

            if (CurrentStreamer != null)
            {
                CurrentStreamer.MetadataChanged -= Streamer_MetadataChanged;

                if (IsPlaying)
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
            NepApp.SongManager.ResetMetadata();
            IsPlaying = false;

            if (MediaTransportControls != null) MediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;

            IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(IsPlaying));
        }

        private async void CurrentPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            var stream = CurrentStream;
            ShutdownPreviousPlaybackSession();

            await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");

            if (!await App.GetIfPrimaryWindowVisibleAsync())
            {
                NepApp.UI.Notifier.ShowErrorToastNotification(stream, "Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");
            }
        }


        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.Buffering:
                case MediaPlaybackState.Opening:
                    //show pause
                    IsPlaying = true;
                    MediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Paused:
                case MediaPlaybackState.None:
                    //show play
                    IsPlaying = false;
                    MediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlaybackState.Playing:
                    //show pause
                    IsPlaying = true;
                    MediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
            }

            IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(IsPlaying));
        }

        private void Streamer_MetadataChanged(object sender, MediaStreamerMetadataChangedEventArgs e)
        {
            if (CurrentStream == null) return;

            if (e.Metadata == null)
            {
                NepApp.SongManager.SetCurrentMetadataToUnknown();
            }
            else
            {
                NepApp.SongManager.HandleMetadata(e.Metadata, CurrentStream);
            }
        }

        public async Task FadeVolumeDownToAsync(double value)
        {
            await CurrentStreamer?.FadeVolumeDownToAsync(value);
        }
        public async Task FadeVolumeUpToAsync(double value)
        {
            await CurrentStreamer?.FadeVolumeUpToAsync(value);
        }

        private void SetIsCasting(bool value)
        {
            if (IsCasting != value)
            {
                IsCasting = value;

                IsCastingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ShowCastingPicker()
        {
            if (CurrentPlayer == null) return;

            CastingDevicePicker picker = new CastingDevicePicker();
            picker.CastingDeviceSelected += Picker_CastingDeviceSelected;
            picker.CastingDevicePickerDismissed += Picker_CastingDevicePickerDismissed;
            picker.Show(Window.Current.Bounds);
        }

        private void Picker_CastingDevicePickerDismissed(CastingDevicePicker sender, object args)
        {
            sender.CastingDevicePickerDismissed -= Picker_CastingDevicePickerDismissed;
            sender.CastingDeviceSelected -= Picker_CastingDeviceSelected;
        }

        private void Picker_CastingDeviceSelected(CastingDevicePicker sender, CastingDeviceSelectedEventArgs args)
        {
            sender.CastingDevicePickerDismissed -= Picker_CastingDevicePickerDismissed;
            sender.CastingDeviceSelected -= Picker_CastingDeviceSelected;

            App.Dispatcher.RunWhenIdleAsync(async () =>
            {
                MediaCastingConnection = args.SelectedCastingDevice.CreateCastingConnection();

                MediaCastingConnection.StateChanged += Connection_StateChanged;
                MediaCastingConnection.ErrorOccurred += Connection_ErrorOccurred;

                await MediaCastingConnection.RequestStartCastingAsync(CurrentPlayer.GetAsCastingSource());
            });
        }

        private async void Connection_ErrorOccurred(CastingConnection sender, CastingConnectionErrorOccurredEventArgs args)
        {
            if (args.ErrorStatus != CastingConnectionErrorStatus.Succeeded)
            {
                sender.StateChanged -= Connection_StateChanged;
                sender.ErrorOccurred -= Connection_ErrorOccurred;

                await sender.DisconnectAsync();
                sender.Dispose();
                MediaCastingConnection = null;

                //IsPlaying = false;

                //ShutdownPreviousPlaybackSession();

                await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", "An error occurred while casting: " + args.Message);

                if (!await App.GetIfPrimaryWindowVisibleAsync())
                {
                    NepApp.UI.Notifier.ShowErrorToastNotification(CurrentStream, "Uh-Oh!", "An error occurred while casting: " + args.Message);
                }
            }
        }

        private void Connection_StateChanged(CastingConnection sender, object args)
        {
            switch (sender.State)
            {
                case CastingConnectionState.Connected:
                case CastingConnectionState.Connecting:
                case CastingConnectionState.Rendering:
                    SetIsCasting(true);
                    break;
                default:
                    SetIsCasting(false);
                    break;
            }
        }
    }
}