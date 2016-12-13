using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Media.Streamers
{
    public static class StreamerFactory
    {
        public static BasicMediaStreamer CreateStreamerFromServerType(StationModelStreamServerType stationType)
        {
            switch(stationType)
            {
                case StationModelStreamServerType.Shoutcast:
                case StationModelStreamServerType.Icecast:
                case StationModelStreamServerType.Radionomy:
                    return new ShoutcastMediaStreamer();
                case StationModelStreamServerType.Direct:
                case StationModelStreamServerType.Other:
                default:
                    return new DirectMediaStreamer();
            }
        }
    }
}
