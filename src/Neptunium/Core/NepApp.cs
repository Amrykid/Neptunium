using Crystal3;
using Kukkii;
using Neptunium.Core;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Settings;
using Neptunium.Core.Stations;
using Neptunium.Core.UI;
using Neptunium.Media;
using Neptunium.Media.Songs;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Neptunium
{
    public static class NepApp
    {
        public interface INepAppFunctionManager { }

        public static NepAppDataCacheManager CacheManager { get; private set; }
        public static NepAppHandoffManager Handoff { get; private set; }
        public static NepAppMediaPlayerManager MediaPlayer { get; private set; }
        public static NepAppSongManager SongManager { get; private set; }
        public static NepAppMetadataManager MetadataManager { get; private set; }
        public static NepAppNetworkManager Network { get; private set; }
        public static NepAppSettingsManager Settings { get; private set; }
        public static NepAppStationsManager Stations { get; private set; }
        public static NepAppUIManager UI { get; private set; }

        public static event EventHandler InitializationComplete;

        public static async Task InitializeAsync()
        {
            CookieJar.ApplicationName = "Neptunium";

            //Hqub.MusicBrainz.API.MyHttpClient.UserAgent = 
            //    "Neptunium/" + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + " ( amrykid@gmail.com )";
            Hqub.MusicBrainz.API.MyHttpClient.UserAgent = "Neptunium/1.0 ( amrykid@gmail.com )";

            CacheManager = new NepAppDataCacheManager();

            await CacheManager.InitializeAsync();

            Settings = new NepAppSettingsManager();
            Stations = new NepAppStationsManager();
            MetadataManager = new NepAppMetadataManager();

            await MetadataManager.InitializeAsync();

            SongManager = new NepAppSongManager();
            MediaPlayer = new NepAppMediaPlayerManager();
            Network = new NepAppNetworkManager();

            Handoff = new NepAppHandoffManager();
            UI = new NepAppUIManager();

            await Handoff.InitializeAsync();

            InitializationComplete?.Invoke(null, EventArgs.Empty);
        }

        public static Binding CreateBinding(INepAppFunctionManager source, string propertyPath, Action<Binding> customizerFunction = null)
        {
            Binding binding = new Windows.UI.Xaml.Data.Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            customizerFunction?.Invoke(binding);
            return binding;
        }
    }
}
