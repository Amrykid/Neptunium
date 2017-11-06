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
        private object originalLockScreen; //currently, there is no universal way to get the original lock screen. this api ( https://docs.microsoft.com/en-us/uwp/api/Windows.System.UserProfile.LockScreen ) only exists on the desktop sku.
        internal NepAppUILockScreenManager()
        {
            
        }

        public async Task<bool> TrySetLockScreenImageFromUri(Uri uri)
        {
            //var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var lockScreenFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("LockScreen Images", CreationCollisionOption.OpenIfExists);

            var originalFileName = uri.Segments.Last().Trim();

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