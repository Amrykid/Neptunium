using Windows.Media.Playback;
using Neptunium.Core.Stations;
using System.Threading.Tasks;

namespace Neptunium.Media
{
    internal interface INepAppMediaStreamer
    {
        void InitializePlayback(MediaPlayer player);
        Task TryConnectAsync(StationStream stream);

        int Volume { get; set; }
        StationItem StationPlaying { get; }
        bool IsPlaying { get; }
        bool IsConnected { get; }
        MediaPlayer Player { get; private set; }
    }
}