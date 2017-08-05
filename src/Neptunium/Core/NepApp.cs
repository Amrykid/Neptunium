using Kukkii;
using Neptunium.Core;
using Neptunium.Core.Stations;
using Neptunium.Core.UI;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Neptunium
{
    public static class NepApp
    {
        public interface INepAppFunctionManager { }

        public static NepAppHandoffManager Handoff { get; private set; }
        public static NepAppMediaPlayerManager Media { get; private set; }
        public static NepAppNetworkManager Network { get; private set; }
        public static NepAppSettingsManager Settings { get; private set; }
        public static NepAppStationsManager Stations { get; private set; }
        public static NepAppUIManager UI { get; private set; }
        public static Task InitializeAsync()
        {
            CookieJar.ApplicationName = "Neptunium";

            Hqub.MusicBrainz.API.MyHttpClient.UserAgent = 
                "Neptunium/" + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Major + " ( amrykid@gmail.com )";

            Settings = new NepAppSettingsManager();
            Stations = new NepAppStationsManager();
            Media = new NepAppMediaPlayerManager();
            Network = new NepAppNetworkManager();
            Handoff = new NepAppHandoffManager();
            UI = new NepAppUIManager();

            return Task.CompletedTask;
        }

        public static Binding CreateBinding(INepAppFunctionManager source, string propertyPath)
        {
            Binding binding = new Windows.UI.Xaml.Data.Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(propertyPath);
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            return binding;
        }
    }
}
