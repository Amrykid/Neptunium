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
    public class DirectMediaStreamer : BasicMediaStreamer
    {
        internal DirectMediaStreamer(): base()
        {
        }

        public override Task ConnectAsync(StationModel station, StationModelStream stream, IEnumerable<KeyValuePair<string, object>> props = null)
        {
            Source = MediaSource.CreateFromUri(new Uri(stream.Url));
            Source.StateChanged += Source_StateChanged;

            Player.Source = Source;

            metadataSubject.OnNext(new BasicSongInfo() { Track = currentTrack, Artist = currentArtist });

            CurrentStation = station;
            CurrentStream = stream;

            return Task.CompletedTask;
        }

        private void Source_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
        {
            IsConnected = args.NewState == MediaSourceState.Opened;
        }

        public override Task DisconnectAsync()
        {
            Source.StateChanged -= Source_StateChanged;

            return Task.CompletedTask;
        }
    }
}
