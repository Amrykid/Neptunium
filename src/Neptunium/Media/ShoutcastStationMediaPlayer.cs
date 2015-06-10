using Neptunium.Data;
using Neptunium.MediaSourceStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Neptunium.Media
{
    public static class ShoutcastStationMediaPlayer
    {
        private static StationModel currentStationModel = null;
        private static ShoutcastMediaSourceStream currentStationMSSWrapper = null;

        public static bool IsPlaying
        {
            get
            {
                var state = BackgroundMediaPlayer.Current.CurrentState;

                return state == MediaPlayerState.Opening || state == MediaPlayerState.Playing || state == MediaPlayerState.Buffering;
            }
        }

        public static async Task PlayStationAsync(StationModel station)
        {
            if (IsPlaying && currentStationMSSWrapper != null)
            {
                BackgroundMediaPlayer.Current.Pause();
                BackgroundMediaPlayer.Shutdown();
                currentStationMSSWrapper.Disconnect();
            }

            currentStationModel = station;

            var stream = station.Streams.First();
            currentStationMSSWrapper = new ShoutcastMediaSourceStream(new Uri(stream.Url));
            await currentStationMSSWrapper.ConnectAsync();

            BackgroundMediaPlayer.Current.SetMediaSource(currentStationMSSWrapper.MediaStreamSource);

            await Task.Delay(500);

            BackgroundMediaPlayer.Current.Play();
        }
    }
}
