using Neptunium.Core;
using Neptunium.Core.Media.Audio;
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
        private double playerVolume = 1.0;

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        internal StationStream CurrentStream { get; private set; }
        public NepAppMediaBluetoothManager Bluetooth { get; private set; }
        public NepAppAudioManager Audio { get; private set; }
        public NepAppMediaSleepTimer SleepTimer { get; private set; }
        private MediaPlayer CurrentPlayer { get; set; }
        public SystemMediaTransportControls MediaTransportControls { get; private set; }
        private MediaPlaybackSession CurrentPlayerSession { get; set; }
        public CastingConnection MediaCastingConnection { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public double Volume
        {
            get { return playerVolume; }
        }
        public bool IsPlaying { get; private set; }
        public bool IsCasting { get; private set; }
        public bool IsMediaEngaged { get; private set; }

        public event EventHandler ConnectingBegin;
        public event EventHandler ConnectingEnd;
        public event EventHandler<NepAppMediaPlayerManagerIsPlayingEventArgs> IsPlayingChanged;
        public event EventHandler<EventArgs> IsCastingChanged;
        public event EventHandler MediaEngagementChanged;
        public event EventHandler<MediaPlayerFailedEventArgs> FatalMediaErrorOccurred;

        public class NepAppMediaPlayerManagerIsPlayingEventArgs : EventArgs
        {
            internal NepAppMediaPlayerManagerIsPlayingEventArgs(bool isPlaying) { IsPlaying = isPlaying; }
            public bool IsPlaying { get; private set; }
        }


        internal NepAppMediaPlayerManager()
        {
            playLock = new SemaphoreSlim(1);
            Bluetooth = new NepAppMediaBluetoothManager(this);
            Audio = new NepAppAudioManager(this);
            SleepTimer = new NepAppMediaSleepTimer(this);
        }

        internal void SetVolume(double value)
        {
            if (value > 1.0 || value < 0.0) throw new ArgumentOutOfRangeException(nameof(value));

            playerVolume = value;

            if (CurrentPlayer != null)
                CurrentPlayer.Volume = playerVolume;
        }

        internal void Pause()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            if (!(bool)CurrentPlayer.PlaybackSession?.CanPause) return;
            CurrentPlayer.Pause();
        }

        internal async void Resume()
        {
            if (CurrentPlayer == null) return;
            if (CurrentPlayer.Source == null) return;
            if (CurrentPlayer.PlaybackSession?.PlaybackState != MediaPlaybackState.Paused) return;

            if (!CurrentStreamer.PollConnection())
            {
                //connection is stale. reconnect

                //NepApp.UI.Overlay.ShowSnackBarMessageAsync("This station has been paused for a while. It will jump ahead.");

                var stream = CurrentStream;
                ShutdownPreviousPlaybackSession();

                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync("Reconnecting...", "Please wait...");
                controller.SetIndeterminate();

                try
                {
                    await TryStreamStationAsync(stream);
                    await controller.CloseAsync();
                }
                catch (Neptunium.Core.NeptuniumException ex)
                {
                    await controller.CloseAsync();
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh! Couldn't reconnect for some reason!", ex.Message);
                    ShutdownPreviousPlaybackSession();
                }
            }
            else
            {
                //resume playback
                CurrentPlayer.Play();
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private void SetMediaEngagement(bool isEngaged)
        {
            bool raise = IsMediaEngaged != isEngaged;
            IsMediaEngaged = isEngaged;
            if (raise)
            {
                RaisePropertyChanged(nameof(IsMediaEngaged));
                MediaEngagementChanged?.Invoke(this, EventArgs.Empty);
            }

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
            CurrentPlayer.Volume = playerVolume;
            CurrentPlayer.MediaFailed += CurrentPlayer_MediaFailed;
            CurrentPlayerSession = CurrentPlayer.PlaybackSession;
            CurrentPlayerSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            streamer.InitializePlayback(CurrentPlayer);

            await CurrentPlayer.WaitForMediaOpenAsync();

            streamer.MetadataChanged += Streamer_MetadataChanged;

            CurrentStreamer = streamer;
            CurrentStream = stream;

            NepApp.SongManager.SetCurrentStation(await NepApp.Stations.GetStationByNameAsync(CurrentStream.ParentStation));

            SetMediaEngagement(true);

            NepApp.Stations.SetLastPlayedStation(stream.ParentStation, DateTime.Now);
            NepApp.SongManager.SetCurrentMetadataToUnknown();

            streamer.Play();

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
            SetMediaEngagement(false);
            IsPlaying = false;

            if (MediaTransportControls != null) MediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;

            IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(IsPlaying));
        }

        private void CurrentPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            var stream = CurrentStream;
            ShutdownPreviousPlaybackSession();

            FatalMediaErrorOccurred?.Invoke(this, args);
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
                try
                {
                    sender.StateChanged -= Connection_StateChanged;
                    sender.ErrorOccurred -= Connection_ErrorOccurred;
                }
                catch (Exception)
                {
                    //We already unhooked.
                }

                await sender.DisconnectAsync();
                sender.Dispose();
                MediaCastingConnection = null;

                //IsPlaying = false;

                //ShutdownPreviousPlaybackSession();

                await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", "An error occurred while casting: " + args.Message);

                if (!await App.GetIfPrimaryWindowVisibleAsync())
                {
                    var currentStation = await NepApp.Stations.GetStationByNameAsync(CurrentStream.ParentStation);
                    NepApp.UI.Notifier.ShowErrorToastNotification(currentStation, "Uh-Oh!", "An error occurred while casting: " + args.Message);
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

                    try
                    {
                        sender.StateChanged -= Connection_StateChanged;
                        sender.ErrorOccurred -= Connection_ErrorOccurred;
                    }
                    catch (Exception)
                    {
                        //We already unhooked.
                    }
                    break;
            }
        }
    }
}