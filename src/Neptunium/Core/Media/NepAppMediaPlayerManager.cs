using Neptunium.Core;
using Neptunium.Core.Stations;
using System.Threading.Tasks;
using static Neptunium.NepApp;

namespace Neptunium.Media
{
    public class NepAppMediaPlayerManager: INepAppFunctionManager
    {
        internal NepAppMediaPlayerManager()
        {

        }

        private INepAppMediaStreamer CreateStreamerForServerFormat(StationStreamServerFormat format)
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

            INepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);
            await streamer.TryConnectAsync(stream); //a failure to connect is caught as a Neptunium.Core.NeptuniumStreamConnectionFailedException by AppShellViewModel
        }
    }
}