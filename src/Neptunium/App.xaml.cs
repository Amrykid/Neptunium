using Crystal3;
using Crystal3.Navigation;
using Kimono.Controls.SnackBar;
using Microsoft.HockeyApp;
using Neptunium.Core;
using Neptunium.Core.UI;
using Neptunium.View;
using Neptunium.View.Dialog;
using Neptunium.ViewModel;
using Neptunium.ViewModel.Dialog;
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
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

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
            App.Current.UnhandledException += Current_UnhandledException;

            //For Xbox One
            this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;
            ElementSoundPlayer.State = ElementSoundPlayerState.Auto;

            Windows.System.MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;
            Windows.System.MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;

            if (!Debugger.IsAttached)
            {
                Microsoft.HockeyApp.HockeyClient.Current.Configure("2f0ab4c93b2341a0a4bbbd5ec98917f9", new TelemetryConfiguration()
                {
                    EnableDiagnostics = true
                }).SetExceptionDescriptionLoader((Exception ex) =>
                {
                    return "Exception HResult: " + ex.HResult.ToString();
                });
            }
        }

        internal static void RegisterUIDialogs()
        {
            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();
            NepApp.UI.Overlay.RegisterDialogFragment<SleepTimerDialogFragment, SleepTimerDialog>();
        }

        private static volatile bool isInBackground = false;
        private static volatile bool isAppVisible = false;
        private static volatile bool isAppFocused = true; //assume true

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
            SnackBarAppearance.Opacity = 1;
            SnackBarAppearance.Transition = new PopupThemeTransition();

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox && !CrystalApplication.GetCurrentAsCrystalApplication().Options.OverridePlatformDetection)
            {
                Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
                    .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
            }

            Window.Current.Activated += Current_Activated;

            await NepApp.InitializeAsync();
        }

        private async Task PostUIInitAsync()
        {
            if ((BackgroundAccess = BackgroundExecutionManager.GetAccessStatus()) == BackgroundAccessStatus.Unspecified)
                BackgroundAccess = await BackgroundExecutionManager.RequestAccessAsync();

            Window.Current.VisibilityChanged += Current_VisibilityChanged;
            isAppVisible = Window.Current.Visible;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            //to deal with the app losing focus on the desktop.
            isAppFocused = e.WindowActivationState != CoreWindowActivationState.Deactivated;
#if DEBUG
            Debug.WriteLine("Window-Activation-State: " + Enum.GetName(typeof(CoreWindowActivationState), e.WindowActivationState));
#endif
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            isAppVisible = e.Visible; //on desktop, this means the app was minimized using the button. on mobile, this can be when the app is suspending or the phone is locked.
        }

        public override async Task OnFreshLaunchAsync(LaunchActivatedEventArgs args)
        {
            WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.NavigateTo<AppShellViewModel>();

            await PostUIInitAsync();
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

        private static Task ExecuteQueryCommandsAsync(Uri uri)
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

                        throw new NotImplementedException();
                    }
            }

            return Task.CompletedTask;
        }

        internal static bool GetIfPrimaryWindowVisible()
        {
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Desktop)
            {
                return isAppVisible && !isInBackground && !isAppFocused;
            }
            else
            {
                return isAppVisible && !isInBackground;
            }
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

        protected override Task OnSuspendingAsync()
        {
            //clears the tile if we're suspending.
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            //ToastNotificationManager.History.Remove(NepAppUIManagerNotifier.SongNotificationTag);

            return Task.CompletedTask;
        }

        protected override async Task OnResumingAsync()
        {
            await base.OnResumingAsync();
        }

        public override Task OnBackgroundActivatedAsync(BackgroundActivatedEventArgs args)
        {
            return Task.CompletedTask;
        }


        private async void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception is NeptuniumException)
            {
                e.Handled = true;

                await NepApp.UI.ShowErrorDialogAsync("Uh-oh! Something went wrong!", e.Exception.Message);
            }
            else
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    HockeyClient.Current.TrackException(e.Exception);
                    HockeyClient.Current.Flush();
                }
                else
                {
                    e.Handled = true;
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
    }
}
