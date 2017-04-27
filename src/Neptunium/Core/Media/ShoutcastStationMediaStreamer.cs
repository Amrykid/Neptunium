using System;
using System.Threading.Tasks;
using Neptunium.Core.Stations;
using Windows.Media.Playback;

namespace Neptunium.Media
{
    internal class ShoutcastStationMediaStreamer : BasicNepAppMediaStreamer
    {
        public override void InitializePlayback(MediaPlayer player)
        {
            throw new NotImplementedException();
        }

        public override Task TryConnectAsync(StationStream stream)
        {
            throw new NotImplementedException();
        }
    }
}