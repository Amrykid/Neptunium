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
            if (!NepApp.Network.IsConnected())

            INepAppMediaStreamer streamer = CreateStreamerForServerFormat(stream.ServerFormat);
            try
            {

            }
            catch (Exception)
            {

            }
        }
    }
}