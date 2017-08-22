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
                streamSource = await ShoutcastStreamFactory.ConnectAsync(stream.StreamUrl, new ShoutcastStreamFactoryConnectionSettings()
                {
                    UserAgent = "Neptunium (http://github.com/Amrykid/Neptunium)",
                    RelativePath = stream.RelativePath
                });

                streamSource.Reconnected += StreamSource_Reconnected;
                streamSource.MetadataChanged += ShoutcastStream_MetadataChanged;
                StreamMediaSource = MediaSource.CreateFromMediaStreamSource(streamSource.MediaStreamSource);
                this.StationPlaying = stream.ParentStation;
            }
            catch (Exception ex)
            {
                throw new Neptunium.Core.NeptuniumStreamConnectionFailedException(stream, ex);
            }
        }

        private void StreamSource_Reconnected(object sender, EventArgs e)
        {
            
        }

        private void ShoutcastStream_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            RaiseMetadataChanged(new Core.Media.Metadata.SongMetadata()
            {
                Artist = e.Artist,
                Track = e.Title,
                StationPlayedOn = this.StationPlaying.Name,
                StationLogo = this.StationPlaying.StationLogoUrl
            });
        }

        public override void Dispose()
        {
            if (streamSource != null)
            {
                streamSource.Disconnect();
                streamSource.Reconnected -= StreamSource_Reconnected;
                streamSource.MetadataChanged -= ShoutcastStream_MetadataChanged;
            }

            base.Dispose();
        }
    }
}