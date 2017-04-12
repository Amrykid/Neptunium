using Crystal3;
using Crystal3.Navigation;
using Microsoft.HockeyApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Gaming.Input;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Xaml;

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

           

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
                    .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
            }


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

        protected override async Task OnApplicationInitializedAsync()
        {
            await NepApp.InitializeAsync();
        }

        private async Task PostUIInitAsync()
        {      
            if ((BackgroundAccess = BackgroundExecutionManager.GetAccessStatus()) == BackgroundAccessStatus.Unspecified)
                BackgroundAccess = await BackgroundExecutionManager.RequestAccessAsync();
        }

        public override Task OnFreshLaunchAsync(LaunchActivatedEventArgs args)
        {
            WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.NavigateTo<AppShellViewModel>();
            return Task.CompletedTask;
        }

        public override async Task OnActivationAsync(IActivatedEventArgs args)
        {
            WindowManager.GetNavigationManagerForCurrentWindow()
                 .RootNavigationService.SafeNavigateTo<AppShellViewModel>();

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
                        var query = uri.Query
                            .Substring(1)
                            .Split('&')
                            .Select(x =>
                                new KeyValuePair<string, string>(
                                    x.Split('=')[0],
                                    x.Split('=')[1])); //remote the "?"

                        var stationName = query.First(x => x.Key.ToLower() == "station").Value;
                        stationName = stationName.Replace("%20", " ");

                        //var station = StationDataManager.Stations.First(x => x.Name == stationName);

                        //if (await StationMediaPlayer.PlayStationAsync(station))
                        //{
                        //    if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.NavigateToStationWhenLaunched])
                        //    {
                        //        WindowManager.GetNavigationManagerForCurrentWindow()
                        //        .GetNavigationServiceFromFrameLevel(FrameLevel.Two)
                        //        .NavigateTo<StationInfoViewModel>(station.Name);
                        //    }
                        //}

                        throw new NotImplementedException();
                    }
            }
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

        }

        protected override async Task OnResumingAsync()
        {
            if (NepApp.Handoff.RemoteSystemAccess == RemoteSystemAccessStatus.Unspecified)
                await NepApp.Handoff.InitializeAsync();
            await NepApp.Handoff.ScanForRemoteSystemsAsync();

            await NepApp.Handoff.CheckForReverseHandoffOpportunitiesAsync();

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
                            case NepAppHandoffManager.ContinuedAppExperienceAppServiceName:
                                NepApp.Handoff.HandleBackgroundActivation(asTD);
                                break;
                        }
                    }
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
