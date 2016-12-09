using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Data;
using Windows.Media.Core;
using System.Reactive.Subjects;
using Neptunium.MediaSourceStream;

namespace Neptunium.Media.Streamers
{
    public class ShoutcastMediaStreamer : IMediaStreamer
    {
        private string currentTrack = "Title";
        private string currentArtist = "Artist";

        public bool IsConnected { get; private set; }

        public MediaSource Source { get; private set; }

        public StationModel CurrentStation { get; private set; }
        public StationModelStream CurrentStream { get; private set; }

        public IObservable<BasicSongInfo> MetadataChanged { get; private set; }
        private Subject<BasicSongInfo> metadataSubject = null;

        public IObservable<Exception> ErrorOccurred { get; private set; }

        private MediaSourceStream.ShoutcastMediaSourceStream shoutcastStream = null;

        internal ShoutcastMediaStreamer()
        {
            currentTrack = "Unknown Song";
            currentArtist = "Unknown Artist";

            metadataSubject = new Subject<BasicSongInfo>();
            MetadataChanged = metadataSubject;

            ErrorOccurred = new Subject<Exception>();

            IsConnected = false;
        }

        public async Task ConnectAsync(StationModel station, StationModelStream stream, IEnumerable<KeyValuePair<string, object>> props = null)
        {
            shoutcastStream = new MediaSourceStream.ShoutcastMediaSourceStream(new Uri(stream.Url), ConvertServerTypeToMediaServerType(stream.ServerType));

            shoutcastStream.MetadataChanged += ShoutcastStream_MetadataChanged;

            try
            {
                await shoutcastStream.ConnectAsync();

                Source = MediaSource.CreateFromMediaStreamSource(shoutcastStream.MediaStreamSource);

                CurrentStation = station;
                CurrentStream = stream;

                IsConnected = true;
            }
            catch (Exception ex)
            {
                shoutcastStream.MetadataChanged -= ShoutcastStream_MetadataChanged;

                ((Subject<Exception>)ErrorOccurred).OnNext(ex);

                IsConnected = false;
            }
        }

        private void ShoutcastStream_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            currentTrack = e.Title;
            currentArtist = e.Artist;

            metadataSubject.OnNext(new BasicSongInfo() { Track = currentTrack, Artist = currentArtist });
        }

        public Task DisconnectAsync()
        {
            if (shoutcastStream != null)
            {
                shoutcastStream.MetadataChanged -= ShoutcastStream_MetadataChanged;
                shoutcastStream.Disconnect();
            }

            return Task.CompletedTask;
        }

        public async Task ReconnectAsync()
        {
            if (CurrentStream == null) throw new Exception("This streamer hasn't been connected before!");

            if (IsConnected)
                await DisconnectAsync();

            await ConnectAsync(CurrentStation, CurrentStream, null);
        }

        public void Dispose()
        {
            if (IsConnected)
                DisconnectAsync().Wait();
        }

        private static ShoutcastServerType ConvertServerTypeToMediaServerType(StationModelStreamServerType currentStationServerType)
        {
            switch (currentStationServerType)
            {
                case StationModelStreamServerType.Shoutcast:
                    return ShoutcastServerType.Shoutcast;
                case StationModelStreamServerType.Radionomy:
                    return ShoutcastServerType.Radionomy;
                default:
                    return ShoutcastServerType.Shoutcast;
            }
        }
    }
}
