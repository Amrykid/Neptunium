using Kukkii;
using Neptunium.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Playback;

namespace Neptunium
{
    public static class NepApp
    {
        public static NepAppHandoffManager Handoff { get; private set; }
        public static NepAppMediaPlayerManager Media { get; private set; }
        public static NepAppSettingsManager Settings { get; private set; }
        public static Task InitializeAsync()
        {
            CookieJar.ApplicationName = "Neptunium";

            Hqub.MusicBrainz.API.MyHttpClient.UserAgent = "Neptunium/" + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Major + " ( amrykid@gmail.com )";

            Settings = new NepAppSettingsManager();
            Media = new NepAppMediaPlayerManager();
            Handoff = new NepAppHandoffManager();


            return Task.CompletedTask;
        }
    }
}
