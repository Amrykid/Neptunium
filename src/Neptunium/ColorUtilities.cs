using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace Neptunium
{
    public static class ColorUtilities
    {
        public static async Task<Color> GetDominantColorAsync(IRandomAccessStream stream)
        {
            //modified code from: http://www.jonathanantoine.com/2013/07/16/winrt-how-to-easily-get-the-dominant-color-of-a-picture/


            //Create a decoder for the image
            var decoder = await BitmapDecoder.CreateAsync(stream);

                //Create a transform to get a 1x1 image
            var myTransform = new BitmapTransform { ScaledHeight = 1, ScaledWidth = 1 };

            //Get the pixel provider
            var pixels = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Ignore,
                myTransform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);

            //Get the bytes of the 1x1 scaled image
            var bytes = pixels.DetachPixelData();

            //read the color 
            var myDominantColor = Color.FromArgb(255, bytes[0], bytes[1], bytes[2]);


            return myDominantColor;
        }
    }
}
