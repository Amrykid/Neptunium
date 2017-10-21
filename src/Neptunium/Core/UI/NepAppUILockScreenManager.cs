using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.Web.Http;

namespace Neptunium.Core.UI
{
    public class NepAppUILockScreenManager
    {
        internal NepAppUILockScreenManager()
        {
        }

        public async Task<bool> TrySetLockScreenImageFromUri(Uri uri)
        {
            var imageCacheFolder = await NepApp.ImageCacheFolder.CreateFolderAsync("LockScreen", CreationCollisionOption.OpenIfExists);

            var originalFileName = uri.Segments.Last().Trim();

            StorageFile fileObject = await imageCacheFolder.TryGetItemAsync(originalFileName) as StorageFile;

            if (fileObject == null)
            {
                if (NepApp.Network.IsConnected)
                {
                    fileObject = await imageCacheFolder.CreateFileAsync(originalFileName);
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

                    fileObject = await imageCacheFolder.TryGetItemAsync(originalFileName) as StorageFile;
                }
                else
                {
                    return false;
                }
            }

            return await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(fileObject);
        }
    }
}