using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Neptunium
{
    public static class MediaElementExtensions
    {
        public static async Task WaitForMediaOpenAsync(this MediaElement player)
        {
            while (player.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Opening)
                await Task.Delay(25);
        }
    }
}
