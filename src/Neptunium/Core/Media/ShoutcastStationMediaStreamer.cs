using System;
using System.Threading.Tasks;
using Neptunium.Core.Stations;
using Windows.Media.Playback;
using Windows.Media.Core;
using UWPShoutcastMSS.Streaming;

namespace Neptunium.Media
{
    internal class ShoutcastStationMediaStreamer : BasicNepAppMediaStreamer
    {
        private ShoutcastMediaSourceStream streamSource = null;
        public override void InitializePlayback(MediaPlayer player)
        {
            Player = player;
            Player.Source = StreamMediaSource;
            this.IsPlaying = true;
        }

        public override async Task TryConnectAsync(StationStream stream)
        {
            streamSource = new ShoutcastMediaSourceStream(stream.StreamUrl, UWPShoutcastMSS.Streaming.ShoutcastServerType.Shoutcast, getMetadata: true);
            if (await streamSource.ConnectAsync())
            {
                streamSource.MetadataChanged += StreamSource_MetadataChanged;
                StreamMediaSource = MediaSource.CreateFromMediaStreamSource(streamSource.MediaStreamSource);
                this.StationPlaying = stream.ParentStation;
            }
            else
            {
                throw new Neptunium.Core.NeptuniumStreamConnectionFailedException(stream);
            }
        }

        private void StreamSource_MetadataChanged(object sender, UWPShoutcastMSS.Streaming.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            RaiseMetadataChanged(new Core.Media.Metadata.SongMetadata()
            {
                Artist = e.Artist,
                Track = e.Title,
                StationPlayedOn = this.StationPlaying
            });
        }

        public override void Dispose()
        {
            if (streamSource != null)
            {
                streamSource.Disconnect();
                streamSource.MetadataChanged -= StreamSource_MetadataChanged;
            }

            base.Dispose();
        }
    }
}