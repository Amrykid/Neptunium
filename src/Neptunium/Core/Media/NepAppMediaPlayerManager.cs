using Neptunium.Core;
using Neptunium.Core.Stations;
using System.Threading.Tasks;
using Windows.Media.Playback;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager: INepAppFunctionManager
    {
        internal NepAppMediaPlayerManager()
        {

        }

        public BasicNepAppMediaStreamer CurrentStreamer { get; private set; }

        private BasicNepAppMediaStreamer CreateStreamerForServerFormat(StationStreamServerFormat format)
        {
            switch(format)
            {
                case StationStreamServerFormat.Direct:
                    return new DirectStationMediaStreamer();
                case StationStreamServerFormat.ShoutIceCast:
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
                CurrentStreamer.Pause();
                CurrentStreamer.Dispose();
            }

            MediaPlayer player = new MediaPlayer();
            player.AudioCategory = MediaPlayerAudioCategory.Media;
            streamer.InitializePlayback(player);

            streamer.Play();

            CurrentStreamer = streamer;
        }


    }
}