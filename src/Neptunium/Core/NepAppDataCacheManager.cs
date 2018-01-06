using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using static Neptunium.NepApp;

namespace Neptunium
{
    public class NepAppDataCacheManager: INepAppFunctionManager
    {
        public enum CacheType
        {
            StationImages,
            TextualDataFiles,
            RoamingDataFiles,
        }

        public StorageFolder ImageCacheFolder { get; private set; }
        public StorageFolder StationImageCacheFolder { get; private set; }
        public StorageFolder DataFilesFolder { get; private set; }
        public StorageFolder RoamingDataFilesFolder { get; private set; }

        private volatile bool isInitialized = false;
        internal NepAppDataCacheManager()
        {

        }

        internal async Task InitializeAsync()
        {
            if (isInitialized) return;

            ImageCacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("ImageCache", CreationCollisionOption.OpenIfExists);
            StationImageCacheFolder = await ImageCacheFolder.CreateFolderAsync("StationLogos", CreationCollisionOption.OpenIfExists);

            DataFilesFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("DataFiles", CreationCollisionOption.OpenIfExists);
            RoamingDataFilesFolder = await ApplicationData.Current.RoamingFolder.CreateFolderAsync("Neptunium", CreationCollisionOption.OpenIfExists);

            isInitialized = true;
        }

        private StorageFolder GetStorageFolderFromCacheType(CacheType cacheType)
        {
            switch(cacheType)
            {
                case CacheType.StationImages:
                    return StationImageCacheFolder;
                case CacheType.TextualDataFiles:
                    return DataFilesFolder;
                case CacheType.RoamingDataFiles:
                    return RoamingDataFilesFolder;
            }

            return null;
        }

        public async Task<Tuple<Uri, StorageFile>> GetOrCacheUriAsync(CacheType cacheType, Uri url, bool preferOnline = false)
        {
            if (!isInitialized) throw new InvalidOperationException();
            if (url == null) throw new ArgumentNullException(nameof(url));

            StorageFolder folder = GetStorageFolderFromCacheType(cacheType);
            if (folder == null) throw new InvalidOperationException();

            var originalFileName = url.Segments.Last().Trim();

            StorageFile fileObject = await folder.TryGetItemAsync(originalFileName) as StorageFile;

            if (fileObject == null || preferOnline)
            {
                if (!NepApp.Network.IsConnected)
                {
                    return new Tuple<Uri, StorageFile>(url, null); //return the online uri for now.
                }
                else
                {
                    fileObject = await folder.CreateFileAsync(originalFileName, CreationCollisionOption.ReplaceExisting);
                    Stream fileStream = await fileObject.OpenStreamForWriteAsync(); //auto disposed by the using statement on the next line
                    using (IOutputStream outputFileStream = fileStream.AsOutputStream())
                    {
                        using (HttpClient http = new HttpClient())
                        {
                            var httpResponse = await http.GetAsync(url);
                            await httpResponse.Content.WriteToStreamAsync(outputFileStream);
                            await outputFileStream.FlushAsync();
                            httpResponse.Dispose();
                        }
                    }
                    //falls through below where it returns our cached copy.
                }
            }

            //return our local copy.

            return new Tuple<Uri, StorageFile>(url, fileObject);
        }

        public async Task<StorageFile> GetOrCacheFileAsync<T>(CacheType cacheType, string fileName, Task<IRandomAccessStream> getterFunctionTask)
        {
            if (!isInitialized) throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (getterFunctionTask == null) throw new ArgumentNullException(nameof(getterFunctionTask));

            StorageFolder folder = GetStorageFolderFromCacheType(cacheType);
            if (folder == null) throw new InvalidOperationException();

            IStorageItem file = null;
            if ((file = await folder.TryGetItemAsync(fileName)) == null)
            {
                //file doesn't exist
                //download it

                var stream = await getterFunctionTask;
                
            }

            return file as StorageFile;
        }
    }
}