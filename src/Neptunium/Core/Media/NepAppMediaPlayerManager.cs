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
        private SystemMediaTransportControls systemMediaTransportControls = null;
        private SemaphoreSlim playLock = null;
        private DispatcherTimer sleepTimer = new DispatcherTimer();

        internal NepAppMediaPlayerManager()
        {
            playLock = new SemaphoreSlim(1);
            History = new SongHistorian();
            History.InitializeAsync();
            Bluetooth = new NepAppMediaBluetoothManager(this);

            sleepTimer.Tick += SleepTimer_Tick;
        }

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        public SongMetadata CurrentMetadata { get; private set; }
        public ExtendedSongMetadata CurrentMetadataExtended { get; private set; }
        internal StationStream CurrentStream { get; private set; }

        public SongHistorian History { get; private set; }
        public NepAppMediaBluetoothManager Bluetooth { get; private set; }

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

        private MediaPlayer CurrentPlayer { get; set; }
        private MediaPlaybackSession CurrentPlayerSession { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

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

        internal bool IsSleepTimerRunning { get { return sleepTimer.IsEnabled; } }

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

            CurrentStreamer = streamer;
            CurrentStream = stream;

            streamer.Play();

            NepApp.Stations.SetLastPlayedStationName(stream.ParentStation.Name);

            if (streamer.SongMetadata == null)
            {
                SetCurrentMetadataToUnknown();
            }
            else
            {
                UpdateMetadata(streamer.SongMetadata);
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
            CurrentMetadataExtended = null;

            RaisePropertyChanged(nameof(CurrentMetadata));
            CurrentMetadataChanged?.Invoke(this, new NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(null));
            IsPlaying = false;

            if (systemMediaTransportControls != null) systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;

            IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(IsPlaying));
        }

        public double Volume
        {
            get { return (double)CurrentPlayer?.Volume; }
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
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Paused:
                case MediaPlaybackState.None:
                    //show play
                    IsPlaying = false;
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaPlaybackState.Playing:
                    //show pause
                    IsPlaying = true;
                    systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
            }

            IsPlayingChanged?.Invoke(this, new NepAppMediaPlayerManagerIsPlayingEventArgs(IsPlaying));
        }

        public event EventHandler ConnectingBegin;
        public event EventHandler ConnectingEnd;

        public bool IsPlaying { get; private set; }
        public bool IsCasting { get; private set; }
        public event EventHandler<NepAppMediaPlayerManagerIsPlayingEventArgs> IsPlayingChanged;
        public event EventHandler<EventArgs> IsCastingChanged;
        public event EventHandler<NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs> CurrentMetadataChanged;
        public event EventHandler<NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs> CurrentMetadataExtendedInfoFound;

        public class NepAppMediaPlayerManagerIsPlayingEventArgs : EventArgs
        {
            internal NepAppMediaPlayerManagerIsPlayingEventArgs(bool isPlaying) { IsPlaying = isPlaying; }
            public bool IsPlaying { get; private set; }
        }

        private async void Streamer_MetadataChanged(object sender, MediaStreamerMetadataChangedEventArgs e)
        {
            if (CurrentStream == null) return;

            if (e.Metadata == null)
            {
                SetCurrentMetadataToUnknown();
                return;
            }

            Func<StationProgram, bool> getStationProgram = x =>
            {
                if (x.Host.ToLower().Equals(e.Metadata.Artist.Trim().ToLower())) return true;
                if (!string.IsNullOrWhiteSpace(x.HostRegexExpression))
                {
                    if (Regex.IsMatch(e.Metadata.Artist, x.HostRegexExpression))
                        return true;
                }

                return false;
            };

            try
            {

                if (CurrentStream.ParentStation?.Programs != null)
                {
                    if (CurrentStream.ParentStation.Programs.Any(getStationProgram))
                    {
                        //we're tuning into a special radio program. this may be a DJ playing remixes, for exmaple.
                        var program = CurrentStream.ParentStation.Programs?.First(getStationProgram);
                        UpdateMetadata(e.Metadata);

                        if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
                        {

                            if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
                                NepApp.UI.Notifier.ShowStationProgrammingToastNotification(program, e.Metadata);
                        }

                        NepApp.UI.Notifier.UpdateLiveTile(new ExtendedSongMetadata(e.Metadata));

                        return;
                    }
                }

                //we're tuned into regular programming/music

                if (CurrentStream.ParentStation != null)
                {
                    if (CurrentStream.ParentStation.StationMessages != null)
                    {
                        if (CurrentStream.ParentStation.StationMessages.Contains(e.Metadata.Track) ||
                            CurrentStream.ParentStation.StationMessages.Contains(e.Metadata.Artist))
                            return;
                    }
                }

                UpdateMetadata(e.Metadata);


                ExtendedSongMetadata newMetadata = await MetadataFinder.FindMetadataAsync(e.Metadata);
                CurrentMetadataExtended = newMetadata;

                CurrentMetadataExtendedInfoFound?.Invoke(this, new NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(newMetadata));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                History.AddSongAsync(newMetadata);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
                {
                    if (CurrentMetadata.Track != newMetadata.Track) return; //the song has changed since we started.

                    if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
                        NepApp.UI.Notifier.ShowSongToastNotification(newMetadata);
                }

                NepApp.UI.Notifier.UpdateLiveTile(newMetadata);
            }
            catch (NullReferenceException ex)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add("Song-Metadata", e.Metadata?.ToString());
                properties.Add("Current-Station", CurrentStream?.ParentStation?.ToString());
                Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, properties);
            }
        }

        private void UpdateMetadata(SongMetadata metadata)
        {
            CurrentMetadata = metadata;

            try
            {
                var updater = systemMediaTransportControls.DisplayUpdater;
                updater.Type = MediaPlaybackType.Music;
                updater.MusicProperties.Title = metadata.Track;
                updater.MusicProperties.Artist = metadata.Artist;
                updater.AppMediaId = metadata.StationPlayedOn.GetHashCode().ToString();
                updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(metadata.StationLogo);
                updater.Update();
            }
            catch (COMException) { }
            catch (Exception ex)
            {

            }

            //this is used for the now playing bar via data binding.
            RaisePropertyChanged(nameof(CurrentMetadata));

            CurrentMetadataChanged?.Invoke(this, new NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(metadata));
        }

        private void SetCurrentMetadataToUnknown()
        {
            //todo make a readonly field
            SongMetadata unknown = new SongMetadata()
            {
                Track = "Unknown Song",
                Artist = "Unknown Artist",
                StationPlayedOn = CurrentStream?.ParentStation?.Name,
                StationLogo = CurrentStream?.ParentStation?.StationLogoUrl,
                IsUnknownMetadata = true
            };


            CurrentMetadata = unknown;
            //this is used for the now playing bar via data binding.
            RaisePropertyChanged(nameof(CurrentMetadata));

            CurrentMetadataChanged?.Invoke(this, new NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs(unknown));
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
                CastingConnection connection = args.SelectedCastingDevice.CreateCastingConnection();

                connection.StateChanged += Connection_StateChanged;
                connection.ErrorOccurred += Connection_ErrorOccurred;

                await connection.RequestStartCastingAsync(CurrentPlayer.GetAsCastingSource());
            });
        }

        private async void Connection_ErrorOccurred(CastingConnection sender, CastingConnectionErrorOccurredEventArgs args)
        {
            if (args.ErrorStatus != CastingConnectionErrorStatus.Succeeded)
            {
                sender.StateChanged -= Connection_StateChanged;
                sender.ErrorOccurred -= Connection_ErrorOccurred;

                IsPlaying = false;

                ShutdownPreviousPlaybackSession();

                await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");

                if (!await App.GetIfPrimaryWindowVisibleAsync())
                {
                    NepApp.UI.Notifier.ShowErrorToastNotification(CurrentStream, "Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");
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