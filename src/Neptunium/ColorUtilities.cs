using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static Color ParseFromHexString(string hexColor)
        {
            //from: https://social.msdn.microsoft.com/Forums/windowsapps/en-US/b296fc19-eaec-457f-a8fa-52896f7a9a3f/uwpuwp-colour-set-by-hex-colout-code?forum=wpdevelop

            //Remove # if present
            if (hexColor.IndexOf('#') != -1)
                hexColor = hexColor.Replace("#", "");
            byte alpha = 0;
            byte red = 0;
            byte green = 0;
            byte blue = 0;

            if (hexColor.Length == 8)
            {
                //#AARRGGBB
                alpha = byte.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                red = byte.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                green = byte.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                blue = byte.Parse(hexColor.Substring(6, 2), NumberStyles.AllowHexSpecifier);
            }

            return Color.FromArgb(alpha, red, green, blue);
        }
    }
}
