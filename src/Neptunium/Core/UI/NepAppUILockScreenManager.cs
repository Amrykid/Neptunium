using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using XamlBrewer.Uwp.Controls;
using XamlBrewer.Uwp.Controls.Helpers;

namespace Neptunium.Core.UI
{
    public class NepAppUILockScreenManager
    {
        private Rect screenBounds = default(Rect);
        private object originalLockScreen; //currently, there is no universal way to get the original lock screen. this api ( https://docs.microsoft.com/en-us/uwp/api/Windows.System.UserProfile.LockScreen ) only exists on the desktop sku.
        private Uri fallBackLockScreenImage = null;
        internal NepAppUILockScreenManager()
        {
            screenBounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;

            var fallBackPath = NepApp.Settings.GetSetting(AppSettings.FallBackLockScreenImageUri) as string;

            if (!string.IsNullOrWhiteSpace(fallBackPath))
                fallBackLockScreenImage = new Uri(fallBackPath);
        }

        public async Task<bool> TrySetLockScreenImageFromUriAsync(Uri uri)
        {
            //var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var lockScreenFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("LockScreen Images", CreationCollisionOption.OpenIfExists);

            var originalFileName = uri.Segments.Last().Trim().Replace(":", "-").Replace(",", "-");

            StorageFile fileObject = await lockScreenFolder.TryGetItemAsync(originalFileName) as StorageFile;

            if (fileObject == null)
            {
                if (NepApp.Network.IsConnected)
                {
                    fileObject = await lockScreenFolder.CreateFileAsync(originalFileName);
                    Stream fileStream = await fileObject.OpenStreamForWriteAsync(); //auto disposed by the using statement on the next line
                    using (IOutputStream outputFileStream = fileStream.AsOutputStream())
                    {
                        using (HttpClient http = new HttpClient())
                        {
                            var httpResponse = await http.GetAsync(uri);
                            await httpResponse.Content.WriteToStreamAsync(outputFileStream);
                            await outputFileStream.FlushAsync();
                            httpResponse.Dispose();
                        }
                    }

                    fileObject = await lockScreenFolder.TryGetItemAsync(originalFileName) as StorageFile;

                    if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                    {
                        //crop it on mobile.

                        var imgProperties = await fileObject.Properties.GetImagePropertiesAsync();

                        double imgHeight = imgProperties.Height;
                        double imgWidth = imgProperties.Width;

                        Point startingPoint = new Point(
                            (imgWidth / 2) - (screenBounds.Width / 2), 0);

                        var croppedBitmap = await await App.Dispatcher.RunAsync(() => CropBitmap.GetCroppedBitmapAsync(fileObject,
                                startingPoint,
                                new Size(screenBounds.Width,
                                    screenBounds.Height), 1));

                        await croppedBitmap.SaveAsync(fileObject);

                        fileObject = await lockScreenFolder.TryGetItemAsync(originalFileName) as StorageFile;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                //from the msdn doc for TrySetLockScreenImageAsync: The operation will fail on mobile if the file size exceeds 2 MBs even if it returns true.

                //Property names: https://msdn.microsoft.com/library/e86e5836-522f-4084-8bb3-4c0d4da9cb26
                var basicProperties = await fileObject.GetBasicPropertiesAsync();
                if (basicProperties.Size >= 2000000) return false;
            }

            return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileObject);
        }

        public async Task<bool> TrySetFallbackLockScreenImageAsync()
        {
            if (fallBackLockScreenImage == null) return false;

            StorageFile fileObject = await StorageFile.GetFileFromPathAsync(fallBackLockScreenImage.ToString());

            if (fileObject != null)
            {
                return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileObject);
            }

            return false;
        }
    }
}