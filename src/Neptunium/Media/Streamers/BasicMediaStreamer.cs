using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Neptunium.Media.Streamers
{
    public abstract class BasicMediaStreamer : IMediaStreamer
    {
        public const string UnknownSong = "Unknown Song";
        public const string UnknownArtist = "Unknown Artist";

        public string CurrentTrack { get; protected set; } = UnknownSong;
        public string CurrentArtist { get; protected set; } = UnknownArtist;

        private SemaphoreSlim volumeLock = null;

        public bool IsConnected { get; protected set; }

        public StationModel CurrentStation { get; protected set; }
        public StationModelStream CurrentStream { get; protected set; }

        public MediaSource Source { get; protected set; }
        public MediaPlayer Player { get; private set; }

        public IObservable<BasicSongInfo> MetadataChanged { get; private set; }
        protected BehaviorSubject<BasicSongInfo> metadataSubject = null;

        public IObservable<Exception> ErrorOccurred { get; private set; }
        protected Subject<Exception> errorSubject = null;

        public BasicMediaStreamer()
        {
            CurrentTrack = "Unknown Song";
            CurrentArtist = "Unknown Artist";

            volumeLock = new SemaphoreSlim(1);

            Player = new MediaPlayer();
            Player.AudioCategory = MediaPlayerAudioCategory.Media;
            Player.Volume = 0.0;

            metadataSubject = new BehaviorSubject<BasicSongInfo>(new BasicSongInfo() { Artist = UnknownArtist, Track = UnknownSong });
            MetadataChanged = metadataSubject;

            errorSubject = new Subject<Exception>();
            ErrorOccurred = errorSubject;

            IsConnected = false;
        }

        public abstract Task ConnectAsync(StationModel station, StationModelStream stream, IEnumerable<KeyValuePair<string, object>> props = null);

        public abstract Task DisconnectAsync();

        public virtual void Dispose()
        {
            if (IsConnected)
                DisconnectAsync().Wait();

            Player.Dispose();

            metadataSubject.OnCompleted();
            errorSubject.OnCompleted();

            errorSubject.Dispose();
            metadataSubject.Dispose();

            volumeLock.Dispose();

            GC.SuppressFinalize(this);
        }

        public virtual async Task ReconnectAsync()
        {
            if (CurrentStream == null) throw new Exception("This streamer hasn't been connected before!");

            await DisconnectAsync();
            await ConnectAsync(CurrentStation, CurrentStream);
        }

        public double Volume { get { return (double)Player?.Volume; } }

        public async Task SetVolumeAsync(double value)
        {
            if (value > 1.0 || value < 0.0) throw new ArgumentOutOfRangeException(nameof(value), actualValue: value, message: "Out of range.");

            await volumeLock.WaitAsync();

            Player.Volume = value;

            volumeLock.Release();
        }

        public async Task FadeVolumeDownToAsync(double value)
        {
            if (value > Player.Volume) throw new ArgumentOutOfRangeException(nameof(value), actualValue: value, message: "Out of range.");

            if (value == Player.Volume) return;

            await volumeLock.WaitAsync();

            var initial = Player.Volume;
            for (double x = initial; x > value; x -= .01)
            {
                await Task.Delay(25);
                Player.Volume = x;
            }

            volumeLock.Release();
        }
        public async Task FadeVolumeUpToAsync(double value)
        {
            if (value < Player.Volume) throw new ArgumentOutOfRangeException(nameof(value), actualValue: value, message: "Out of range.");

            if (value == Player.Volume) return;

            await volumeLock.WaitAsync();

            var initial = Player.Volume;
            for (double x = initial; x < value; x += .01)
            {
                await Task.Delay(25);
                Player.Volume = x;
            }

            volumeLock.Release();
        }
    }
}
