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
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Notifications;
using Neptunium.Managers.Songs;

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

            CookieJar.ApplicationName = "Neptunium";

            Hqub.MusicBrainz.API.MyHttpClient.UserAgent = "Neptunium/" + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Major + " ( amrykid@gmail.com )";

            //initialize app settings
            //todo add all settings

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.ShowSongNotifications))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.ShowSongNotifications, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.TryToFindSongMetadata))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.TryToFindSongMetadata, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.NavigateToStationWhenLaunched))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.NavigateToStationWhenLaunched, true);
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(AppSettings.MediaBarMatchStationColor))
                ApplicationData.Current.LocalSettings.Values.Add(AppSettings.MediaBarMatchStationColor, true);

            Windows.System.MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;
            Windows.System.MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;


            if (!Debugger.IsAttached)
            {
                App.Current.UnhandledException += Current_UnhandledException;

                Microsoft.HockeyApp.HockeyClient.Current.Configure("2f0ab4c93b2341a0a4bbbd5ec98917f9", new TelemetryConfiguration()
                {
                    EnableDiagnostics = true
                }).SetExceptionDescriptionLoader((Exception ex) =>
                {
                    return "Exception HResult: " + ex.HResult.ToString();
                });
            }
            else
            {
#if DEBUG
                App.Current.UnhandledException += Current_UnhandledException;
#endif
            }
        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!Debugger.IsAttached)
            {
                try
                {
                    HockeyClient.Current.TrackException(e.Exception);
                    HockeyClient.Current.Flush();
                }
                catch (NullReferenceException) { }

                e.Handled = true;
            }
            else
            {
                Debugger.Break();
                e.Handled = true;
            }
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
#if DEBUG
                //todo not actually a todo but a PSA: to do Xbox testing on PC, set a breakpoint on Gamepads.Count or uncomment the Debugger.Break code.
                //then however over .Count twice with a breakpoint once to force system to recognize xbox one controller.
                //if (Debugger.IsAttached)
                //    Debugger.Break();
                if (Gamepad.Gamepads.Count > 0)
                {
                    this.Options.OverridePlatform(Crystal3.Core.Platform.Xbox);
                }
#endif
            }

            this.Options.HandleSystemBackNavigation = true;
        }

        private static async Task TryInitOrHealCookieContainerAsync(Kukkii.Core.IPersistentCookieContainer container)
        {
            if (container == null) return;

            try
            {
                await container.InitializeAsync();
            }
            catch (CacheCannotBeLoadedException)
            {
                await container.RegenerateCacheAsync();

                var msg = "Regenerating data...";
                if (App.MessageQueue.Contains(msg))
                    App.MessageQueue.Enqueue(msg);
            }
        }

        private static bool coreInitialized = false;
        private static async Task CoreInitAsync()
        {
            if (coreInitialized) return;

            try
            {
                await TryInitOrHealCookieContainerAsync(CookieJar.Device);
                await TryInitOrHealCookieContainerAsync(CookieJar.DeviceCache);
            }
            catch (Exception) { }

            await StationDataManager.InitializeAsync();

            await SongManager.InitializeAsync();
            await StationMediaPlayer.InitializeAsync();

            coreInitialized = true;
        }

        private async Task PostUIInitAsync()
        {
            SnackBarAppearance.Opacity = 1.0;

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                SnackBarAppearance.MessageFontSize = 14;
                SnackBarAppearance.Transition = new PopupThemeTransition();

                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
                    .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
            }
            else
            {
                SnackBarAppearance.MessageFontSize = 12;
                SnackBarAppearance.Transition = new AddDeleteThemeTransition();
            }

            FragmentManager.RegisterFragmentView<StationInfoViewSongHistoryFragment, StationInfoViewSongHistoryFragmentView>();
            FragmentManager.RegisterFragmentView<NowPlayingViewFragment, NowPlayingInfoBar>();

            if ((BackgroundAccess = BackgroundExecutionManager.GetAccessStatus()) == BackgroundAccessStatus.Unspecified)
                BackgroundAccess = await BackgroundExecutionManager.RequestAccessAsync();

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
            await FullInitAsync();

            WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.NavigateTo<AppShellViewModel>();

            await Task.CompletedTask;
        }

        public override async Task OnActivationAsync(IActivatedEventArgs args)
        {
            if (!WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.IsNavigatedTo<AppShellViewModel>())
            {
                await FullInitAsync();

                WindowManager.GetNavigationManagerForCurrentWindow()
                    .RootNavigationService.NavigateTo<AppShellViewModel>();
            }

            if (args.Kind == ActivationKind.Protocol)
            {
                var pargs = args as ProtocolActivatedEventArgs;

                var uri = pargs.Uri;
                await ExecuteQueryCommandsAsync(uri);
            }
            else if (args.Kind == ActivationKind.Launch && (args is LaunchActivatedEventArgs))
            {
                //tile activation. crystal wouldn't call here otherwise.

                var largs = args as LaunchActivatedEventArgs;
                if (!string.IsNullOrWhiteSpace(largs.Arguments))
                {
                    await ExecuteQueryCommandsAsync(new Uri("nep:" + largs.Arguments));
                }
            }
        }

        private static async Task ExecuteQueryCommandsAsync(Uri uri)
        {
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

                            var station = StationDataManager.Stations.First(x => x.Name == stationName);

                            if (await StationMediaPlayer.PlayStationAsync(station))
                            {
                                if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.NavigateToStationWhenLaunched])
                                {
                                    WindowManager.GetNavigationManagerForCurrentWindow()
                                    .GetNavigationServiceFromFrameLevel(FrameLevel.Two)
                                    .NavigateTo<StationInfoViewModel>(station.Name);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    break;
            }
        }

        private async Task FullInitAsync()
        {
            await CoreInitAsync();
            await PostUIInitAsync();
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
            using (ExtendedExecutionSession session = new ExtendedExecutionSession())
            {
                session.Reason = ExtendedExecutionReason.SavingData;

                ContinuedAppExperienceManager.StopWatchingForRemoteSystems();
                await SongManager.FlushAsync();


                var extendedAccess = await session.RequestExtensionAsync();


                //clears the tile if we're suspending.
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();

                if (extendedAccess == ExtendedExecutionResult.Allowed)
                {
                    await Task.WhenAll(CookieJar.DeviceCache.FlushAsync(), CookieJar.Device.FlushAsync());
                }
            }

            await base.OnSuspendingAsync();
        }

        protected override async Task OnResumingAsync()
        {

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
