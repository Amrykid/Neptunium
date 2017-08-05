using Neptunium.Core;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Media.Playback;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager: INepAppFunctionManager, INotifyPropertyChanged
    {
        internal NepAppMediaPlayerManager()
        {

        }

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }
        public SongMetadata CurrentMetadata { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BasicNepAppMediaStreamer CreateStreamerForServerFormat(StationStreamServerFormat format)
        {
            switch(format)
            {
                case StationStreamServerFormat.Direct:
                    return new DirectStationMediaStreamer();
                case StationStreamServerFormat.Shoutcast:
                case StationStreamServerFormat.Icecast:
                    return new ShoutcastStationMediaStreamer();
            }

            return null;
        }

        public async Task TryStreamStationAsync(StationStream stream)
        {
            if (!NepApp.Network.IsConnected) throw new NeptuniumNetworkConnectionRequiredException();

            BasicNepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);
            await streamer.TryConnectAsync(stream); //a failure to connect is caught as a Neptunium.Core.NeptuniumStreamConnectionFailedException by AppShellViewModel

            if (CurrentStreamer != null)
            {
                CurrentStreamer.MetadataChanged -= Streamer_MetadataChanged;

                CurrentStreamer.Pause();
                CurrentStreamer.Dispose();
            }

            MediaPlayer player = new MediaPlayer();
            player.AudioCategory = MediaPlayerAudioCategory.Media;
            streamer.InitializePlayback(player);

            streamer.MetadataChanged += Streamer_MetadataChanged;

            UpdateMetadata(streamer.SongMetadata);

            streamer.Play();

            CurrentStreamer = streamer;
        }

        private void Streamer_MetadataChanged(object sender, MediaStreamerMetadataChangedEventArgs e)
        {
            UpdateMetadata(e.Metadata);
        }

        private void UpdateMetadata(SongMetadata metadata)
        {
            CurrentMetadata = metadata;
            RaisePropertyChanged(nameof(CurrentMetadata));
        }
    }
}