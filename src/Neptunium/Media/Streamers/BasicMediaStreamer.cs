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
    public abstract class BasicMediaStreamer: IMediaStreamer
    {
        public string CurrentTrack { get; protected set; } = "Unknown Song";
        public string CurrentArtist { get; protected set; } = "Unknown Artist";

        private SemaphoreSlim volumeLock = null;

        public bool IsConnected { get; protected set; }

        public StationModel CurrentStation { get; protected set; }
        public StationModelStream CurrentStream { get; protected set; }

        public MediaSource Source { get; protected set; }
        public MediaPlayer Player { get; private set; }

        public IObservable<BasicSongInfo> MetadataChanged { get; private set; }
        protected BehaviorSubject<BasicSongInfo> metadataSubject = null;

        public IObservable<Exception> ErrorOccurred { get; private set; }

        public BasicMediaStreamer()
        {
            CurrentTrack = "Unknown Song";
            CurrentArtist = "Unknown Artist";

            volumeLock = new SemaphoreSlim(1);

            Player = new MediaPlayer();
            Player.AudioCategory = MediaPlayerAudioCategory.Media;
            Player.Volume = 0.0;

            metadataSubject = new BehaviorSubject<BasicSongInfo>(new BasicSongInfo() { Artist = CurrentArtist, Track = CurrentTrack });
            MetadataChanged = metadataSubject;

            ErrorOccurred = new Subject<Exception>();

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
            ((Subject<Exception>)ErrorOccurred).OnCompleted();

            volumeLock.Dispose();
        }

        public virtual Task ReconnectAsync()
        {
            if (CurrentStream == null) throw new Exception("This streamer hasn't been connected before!");

            return ConnectAsync(CurrentStation, CurrentStream);
        }

        public double Volume { get { return (double)Player?.Volume; } }

        public async Task FadeVolumeDownToAsync(double value)
        {
            if (value > Player.Volume) throw new ArgumentOutOfRangeException(nameof(value));

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
            if (value < Player.Volume) throw new ArgumentOutOfRangeException(nameof(value));

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
