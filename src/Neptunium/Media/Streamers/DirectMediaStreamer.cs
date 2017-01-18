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
            Source = MediaSource.CreateFromUri(new Uri(stream.Url.Trim()));
            Source.StateChanged += Source_StateChanged;

            Player.Source = Source;

            metadataSubject.OnNext(new BasicSongInfo() { Track = CurrentTrack, Artist = CurrentArtist });

            CurrentStation = station;
            CurrentStream = stream;

            return Task.CompletedTask;
        }

        private void Source_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
        {
            IsConnected = args.NewState == MediaSourceState.Opened || args.NewState == MediaSourceState.Opening;
        }

        public override Task DisconnectAsync()
        {
            if (Source != null)
            {
                Source.StateChanged -= Source_StateChanged;
                Source.Dispose();
                Source = null;
            }

            return Task.CompletedTask;
        }
    }
}
