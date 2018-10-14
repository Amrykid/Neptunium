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

namespace Neptunium.Core.UI
{
    public class NepAppUILockScreenManager
    {
        private Rect screenBounds = default(Rect);
        //currently, there is no universal way to get the original lock screen. this api ( https://docs.microsoft.com/en-us/uwp/api/Windows.System.UserProfile.LockScreen ) only exists on the desktop sku.
        //private object originalLockScreen;
        private Uri fallBackLockScreenImage = null;
        private StorageFolder lockScreenFolder = null;
        internal NepAppUILockScreenManager()
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            screenBounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;

            var fallBackFileName = NepApp.Settings.GetSetting(AppSettings.FallBackLockScreenImageUri) as string;

            lockScreenFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("LockScreen Images", CreationCollisionOption.OpenIfExists);

            if (!string.IsNullOrWhiteSpace(fallBackFileName))
            {
                var file = await lockScreenFolder.GetFileAsync(fallBackFileName);
                fallBackLockScreenImage = new Uri(file.Path);
            }

            if (UserProfilePersonalizationSettings.IsSupported())
            {
                NepApp.SongManager.ArtworkProcessor.SongArtworkAvailable += SongManager_SongArtworkAvailable;
                NepApp.SongManager.ArtworkProcessor.NoSongArtworkAvailable += SongManager_NoSongArtworkAvailable;
            }
        }


        private async void SongManager_NoSongArtworkAvailable(object sender, Neptunium.Media.Songs.NepAppSongMetadataArtworkEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt))
            {
                //sets the fallback lockscreen image when we don't have any artwork available.
                if (e.ArtworkType == Neptunium.Media.Songs.NepAppSongMetadataBackground.Artist)
                {
                    try
                    {
                        await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private async void SongManager_SongArtworkAvailable(object sender, Neptunium.Media.Songs.NepAppSongMetadataArtworkEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt))
            {
                if (e.ArtworkType == Neptunium.Media.Songs.NepAppSongMetadataBackground.Artist)
                {
                    try
                    {
                        bool result = await NepApp.UI.LockScreen.TrySetLockScreenImageFromUriAsync(e.ArtworkUri);

                        if (!result)
                        {
                            await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                        }
                    }
                    catch (Exception)
                    {
                        //todo make and set an image that represents the lack of artwork. maybe a dark image with the app logo?
                        //maybe allow the user to set an image to use in this case.

                        await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                    }
                }
            }
        }


        #region Functions
        public async Task<bool> TrySetLockScreenImageFromUriAsync(Uri uri)
        {
            //var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);

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

                        /*var imgProperties = await fileObject.Properties.GetImagePropertiesAsync();

                        double imgHeight = imgProperties.Height;
                        double imgWidth = imgProperties.Width;*/


                        await CropImageAsync(fileObject, uri).ConfigureAwait(false);

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

        private async Task CropImageAsync(StorageFile fileObject, Uri originalUri)
        {
            using (var stream = await fileObject.OpenAsync(FileAccessMode.ReadWrite))
            {
                try
                {
                    stream.Seek(0);

                    BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(stream);

                    uint height = (uint)Math.Min(bitmapDecoder.OrientedPixelHeight, screenBounds.Height);
                    uint width = (uint)Math.Min(bitmapDecoder.OrientedPixelWidth, screenBounds.Width);

                    Point startingPoint = new Point(
                        Math.Round((width / 2) - (screenBounds.Width / 2)), 0);

                    var softwareBitmap = bitmapDecoder.GetSoftwareBitmapAsync();

                    BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateForTranscodingAsync(stream, bitmapDecoder);

                    //set the cropped area
                    bitmapEncoder.BitmapTransform.Bounds = new BitmapBounds()
                    {
                        X = (uint)startingPoint.X,
                        Y = (uint)startingPoint.Y,
                        Width = width,
                        Height = height
                    };

                    await bitmapEncoder.FlushAsync();
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties.Add("Original-URL", originalUri.ToString());
                    properties.Add("Screen-Bounds", screenBounds.ToString());
                    Microsoft.HockeyApp.HockeyClient.Current.TrackException(ex, properties);
                }
            }
        }

        internal async Task SetFallbackImageAsync(StorageFile file)
        {
            if (file == null) return;

            var newFile = await file.CopyAsync(lockScreenFolder);

            fallBackLockScreenImage = new Uri(newFile.Path);
            NepApp.Settings.SetSetting(AppSettings.FallBackLockScreenImageUri, newFile.Name);
        }

        public Uri FallbackLockScreenImage { get { return fallBackLockScreenImage; } }

        public async Task<bool> TrySetFallbackLockScreenImageAsync()
        {
            if (fallBackLockScreenImage == null) return false;

            var name = fallBackLockScreenImage.Segments.Last();
            StorageFile fileObject = await lockScreenFolder.GetFileAsync(name);

            if (fileObject != null)
            {
                return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileObject);
            }

            return false;
        }
        #endregion
    }
}