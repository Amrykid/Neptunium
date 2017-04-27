using Windows.Media.Playback;
using Neptunium.Core.Stations;
using System.Threading.Tasks;
using System;

namespace Neptunium.Media
{
    internal interface INepAppMediaStreamer
    {
        void InitializePlayback(MediaPlayer player);
        Task TryConnectAsync(StationStream stream);

        double Volume { get; set; }
        StationItem StationPlaying { get; }
        bool IsPlaying { get; }
        bool IsConnected { get; }
        MediaPlayer Player { get; }
    }

    public abstract class BasicNepAppMediaStreamer : INepAppMediaStreamer
    {
        public bool IsConnected { get; private set; }

        public bool IsPlaying { get; private set; }

        public MediaPlayer Player { get; private set; }

        public StationItem StationPlaying { get; private set; }

        public double Volume
        {
            get
            {
                if (Player == null) throw new InvalidOperationException();
                return (double)Player.Volume;
            }

            set
            {
                if (Player == null) throw new InvalidOperationException();
                Player.Volume = value;
            }
        }

        public abstract void InitializePlayback(MediaPlayer player);
        public abstract Task TryConnectAsync(StationStream stream);
    }
}