using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Neptunium
{
    public static class MediaExtensions
    {
        public static async Task WaitForMediaOpenAsync(this MediaElement player)
        {
            while (player.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Opening)
                await Task.Delay(25);
        }

        public static async Task WaitForMediaOpenAsync(this MediaPlayer player)
        {
            while (player.PlaybackSession?.PlaybackState == MediaPlaybackState.Opening)
                await Task.Delay(25);
        }
    }
}
