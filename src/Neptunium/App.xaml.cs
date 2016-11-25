using Crystal3;
using Crystal3.Navigation;
using Neptunium.Data;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.ViewManagement;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Neptunium.Shared;
using Windows.Media.Playback;
using System.Diagnostics;
using Neptunium.Media;
using Neptunium.Logging;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Neptunium.Managers;
using Windows.Storage;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.System.RemoteSystems;
using Kukkii;
using Windows.System;
using Windows.Networking.Connectivity;
using Windows.Gaming.Input;
using Kimono.Controls.SnackBar;
using Microsoft.HockeyApp;
using Windows.UI.Xaml.Media.Animation;
using Neptunium.Fragments;
using Neptunium.View.Fragments;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=402347&clcid=0x409

namespace Neptunium
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : CrystalApplication
    {
        public static BackgroundAccessStatus BackgroundAccess { get; private set; }
        public static Queue<string> MessageQueue { get; private set; } = new Queue<string>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //For Xbox One
            this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;
            ElementSoundPlayer.State = ElementSoundPlayerState.Auto;

            CoreInit();

            Windows.System.MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;
            Windows.System.MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;

#if !DEBUG
            Microsoft.HockeyApp.HockeyClient.Current.Configure("2f0ab4c93b2341a0a4bbbd5ec98917f9", new TelemetryConfiguration()
            {
                EnableDiagnostics = true
            });
#endif
        }

        private static volatile bool isInBackground = false;

        protected override Task OnForegroundingAsync()
        {
            isInBackground = false;
            return base.OnForegroundingAsync();
        }
        protected override Task OnBackgroundingAsync()
        {
            isInBackground = true;
            return base.OnBackgroundingAsync();
        }


        #region Memory reduction stuff based on https://msdn.microsoft.com/en-us/windows/uwp/audio-video-camera/background-audio
        private void MemoryManager_AppMemoryUsageLimitChanging(object sender, AppMemoryUsageLimitChangingEventArgs e)
        {
            if (MemoryManager.AppMemoryUsage >= e.NewLimit)
            {
                ReduceMemoryUsage(e.NewLimit);
            }
        }

        private void MemoryManager_AppMemoryUsageIncreased(object sender, object e)
        {
            var level = MemoryManager.AppMemoryUsageLevel;

            if (level == AppMemoryUsageLevel.OverLimit || level == AppMemoryUsageLevel.High)
            {
                ReduceMemoryUsage(MemoryManager.AppMemoryUsageLimit);
            }
        }

        public void ReduceMemoryUsage(ulong limit)
        {
            if (isInBackground)
            {
                if (StationDataManager.IsInitialized)
                    StationDataManager.DeinitializeAsync();

                GC.Collect();
            }
        }
        #endregion

        protected override void OnConfigure()
        {
            base.OnConfigure();

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Desktop)
            {
                //#if DEBUG
                //                //however over .Count twice with a breakpoint once to force system to recognize xbox one controller.
                //                if (Debugger.IsAttached)
                //                    Debugger.Break();
                //                if (Gamepad.Gamepads.Count > 0)
                //                {
                //                    this.Options.OverridePlatform(Crystal3.Core.Platform.Xbox);
                //                }
                //#endif
            }

            this.Options.HandleSystemBackNavigation = true;
        }

        private static async void CoreInit()
        {
            CookieJar.ApplicationName = "Neptunium";

            if ((BackgroundAccess = BackgroundExecutionManager.GetAccessStatus()) == BackgroundAccessStatus.Unspecified)
                BackgroundAccess = await BackgroundExecutionManager.RequestAccessAsync();

            //initialize app settings
            //todo add all settings

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.ShowSongNotifications))
#if RELEASE
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.ShowSongNotifications, false);
#else
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.ShowSongNotifications, true);
#endif
            await LogManager.InitializeAsync();
            await StationMediaPlayer.InitializeAsync();

            await SongHistoryManager.InitializeAsync();

            Hqub.MusicBrainz.API.MyHttpClient.UserAgent = "Neptunium/0.1 ( amrykid@gmail.com )";

            FragmentManager.RegisterFragmentView<StationInfoViewSongHistoryFragment, StationInfoViewSongHistoryFragmentView>();
        }

        private async Task PostUIInitAsync()
        {
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                SnackBarAppearance.Opacity = 1.0;
                SnackBarAppearance.MessageFontSize = 14;
                SnackBarAppearance.Transition = new PopupThemeTransition();

                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
                    .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
            }
            else
            {
                SnackBarAppearance.Opacity = 0.8;
                SnackBarAppearance.MessageFontSize = 12;
                SnackBarAppearance.Transition = new AddDeleteThemeTransition();
            }

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                if (!CarModeManager.IsInitialized)
                    await CarModeManager.InitializeAsync();
            }

            //Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            if (!ContinuedAppExperienceManager.IsInitialized)
                await ContinuedAppExperienceManager.InitializeAsync();

        }

        public override async Task OnFreshLaunchAsync(LaunchActivatedEventArgs args)
        {
            await PostUIInitAsync();

            LogManager.Info(typeof(App), "Application Launching");
            WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.NavigateTo<AppShellViewModel>();

            await Task.CompletedTask;
        }

        public override async Task OnActivationAsync(IActivatedEventArgs args)
        {

            if (!WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.IsNavigatedTo<AppShellViewModel>())
            {
                await PostUIInitAsync();

                WindowManager.GetNavigationManagerForCurrentWindow()
                    .RootNavigationService.NavigateTo<AppShellViewModel>();
            }

            if (args.Kind == ActivationKind.Protocol)
            {
                var pargs = args as ProtocolActivatedEventArgs;

                var uri = pargs.Uri;
                switch (uri.LocalPath.ToLower())
                {
                    case "play-station":
                        {
                            try
                            {
                                var query = uri.Query
                                    .Substring(1)
                                    .Split('&')
                                    .Select(x =>
                                        new KeyValuePair<string, string>(
                                            x.Split('=')[0],
                                            x.Split('=')[1])); //remote the "?"

                                var stationName = query.First(x => x.Key.ToLower() == "station").Value;
                                stationName = stationName.Replace("%20", " ");

                                if (!StationDataManager.IsInitialized)
                                    await StationDataManager.InitializeAsync();

                                var station = StationDataManager.Stations.First(x => x.Name == stationName);

                                await StationMediaPlayer.PlayStationAsync(station);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        break;
                }
            }

            await Task.CompletedTask;
        }

        internal static bool GetIfPrimaryWindowVisible()
        {
            return Windows.UI.Xaml.Window.Current.Visible || !isInBackground;
        }
        internal static async Task<bool> GetIfPrimaryWindowVisibleAsync()
        {
            return await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                return App.GetIfPrimaryWindowVisible();
            });
        }

        public static bool IsInternetConnected()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = (connections != null) &&
                (connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
            return internet;
        }
        public static bool IsUnrestrictiveInternetConnection()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile != null)
            {
                var cost = profile.GetConnectionCost();

                return cost.NetworkCostType == NetworkCostType.Unrestricted
                    || (cost.NetworkCostType == NetworkCostType.Fixed && cost.ApproachingDataLimit == false);
            }

            return true;
        }

        protected override async Task OnSuspendingAsync()
        {
            LogManager.Info(typeof(App), "Application Suspending");

            ContinuedAppExperienceManager.StopWatchingForRemoteSystems();
            await SongHistoryManager.FlushAsync();

            await base.OnSuspendingAsync();
        }

        protected override async Task OnResumingAsync()
        {
            //await LogManager.InitializeAsync();

            LogManager.Info(typeof(App), "Application Resuming");

            if (ContinuedAppExperienceManager.RemoteSystemAccess == RemoteSystemAccessStatus.Unspecified)
                await ContinuedAppExperienceManager.InitializeAsync();
            ContinuedAppExperienceManager.StartWatchingForRemoteSystems();

            ContinuedAppExperienceManager.CheckForReverseHandoffOpportunities();

            await base.OnResumingAsync();
        }

        public override Task OnBackgroundActivatedAsync(BackgroundActivatedEventArgs args)
        {
            switch (args.TaskInstance.Task.Name)
            {
                default:
                    if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
                    {
                        var asTD = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;

                        switch (asTD.Name)
                        {
                            case ContinuedAppExperienceManager.ContinuedAppExperienceAppServiceName:
                                ContinuedAppExperienceManager.HandleBackgroundActivation(asTD);
                                break;
                        }
                    }
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
