using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;
using Windows.Media.Core;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Neptunium.Media.Streamers
{
    public class DirectMediaStreamer : IMediaStreamer
    {
        private string currentTrack = "Title";
        private string currentArtist = "Artist";

        public bool IsConnected { get; private set; }

        public StationModel CurrentStation { get; private set; }
        public StationModelStream CurrentStream { get; private set; }

        public MediaSource Source { get; private set; }

        public IObservable<BasicSongInfo> MetadataChanged { get; private set; }
        private Subject<BasicSongInfo> metadataSubject = null;

        public IObservable<Exception> ErrorOccurred { get; private set; }

        internal DirectMediaStreamer()
        {
            currentTrack = "Unknown Song";
            currentArtist = "Unknown Artist";

            metadataSubject = new Subject<BasicSongInfo>();
            MetadataChanged = metadataSubject;

            ErrorOccurred = new Subject<Exception>();

            IsConnected = false;
        }

        public Task ConnectAsync(StationModel station, StationModelStream stream, IEnumerable<KeyValuePair<string, object>> props = null)
        {
            Source = MediaSource.CreateFromUri(new Uri(stream.Url));
            Source.StateChanged += Source_StateChanged;

            metadataSubject.OnNext(new BasicSongInfo() { Track = currentTrack, Artist = currentArtist });

            CurrentStation = station;
            CurrentStream = stream;

            return Task.CompletedTask;
        }

        private void Source_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
        {
            IsConnected = args.NewState == MediaSourceState.Opened;
        }

        public Task DisconnectAsync()
        {
            Source.StateChanged -= Source_StateChanged;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (IsConnected)
                DisconnectAsync().Wait();

            metadataSubject.OnCompleted();
            ((Subject<Exception>)ErrorOccurred).OnCompleted();
        }

        public Task ReconnectAsync()
        {
            if (CurrentStream == null) throw new Exception("This streamer hasn't been connected before!");

            return ConnectAsync(CurrentStation, CurrentStream);
        }
    }
}
