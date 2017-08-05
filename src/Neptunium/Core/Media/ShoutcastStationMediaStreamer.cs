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
        private ShoutcastStream streamSource = null;
        public override void InitializePlayback(MediaPlayer player)
        {
            Player = player;
            Player.Source = StreamMediaSource;
            this.IsPlaying = true;
        }

        public override async Task TryConnectAsync(StationStream stream)
        {      
            try
            {
                streamSource = await ShoutcastStreamFactory.ConnectAsync(stream.StreamUrl);
                streamSource.MetadataChanged += ShoutcastStream_MetadataChanged;
                StreamMediaSource = MediaSource.CreateFromMediaStreamSource(streamSource.MediaStreamSource);
                this.StationPlaying = stream.ParentStation;
            }
            catch(Exception ex)
            {
                throw new Neptunium.Core.NeptuniumStreamConnectionFailedException(stream);
            }
        }

        private void ShoutcastStream_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
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
                streamSource.MetadataChanged -= ShoutcastStream_MetadataChanged;
            }

            base.Dispose();
        }
    }
}