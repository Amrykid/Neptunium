using Windows.Media.Playback;
using Neptunium.Core.Stations;
using System.Threading.Tasks;
using System;
using Windows.Media.Core;

namespace Neptunium.Media
{
    internal interface INepAppMediaStreamer: IDisposable
    {
        void InitializePlayback(MediaPlayer player);
        Task TryConnectAsync(StationStream stream);

        double Volume { get; set; }
        StationItem StationPlaying { get; }
        bool IsPlaying { get; }
        bool IsConnected { get; }
        MediaPlayer Player { get; }
        MediaSource StreamMediaSource { get; }

        void Play();
        void Pause();
    }

    public abstract class BasicNepAppMediaStreamer : INepAppMediaStreamer
    {
        public MediaSource StreamMediaSource { get; protected set; }
        public virtual bool IsConnected { get { return (bool)StreamMediaSource?.IsOpen; } }

        public bool IsPlaying { get; protected set; }

        public MediaPlayer Player { get; protected set; }

        public StationItem StationPlaying { get; protected set; }

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

        public void Dispose()
        {
            if (Player != null)
            {
                Player.Dispose();
                Player = null;
            }

            if (StreamMediaSource != null)
            {
                StreamMediaSource.Dispose();
            }
        }

        public void Play()
        {
            if (Player == null) throw new InvalidOperationException();

            Player.Play();
        }

        public void Pause()
        {
            if (Player == null) throw new InvalidOperationException();

            Player.Pause();
        }
    }
}