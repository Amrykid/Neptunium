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

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=402347&clcid=0x409

namespace Neptunium
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : CrystalApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if DEBUG
            Application.Current.UnhandledException += Current_UnhandledException;
#endif
            CoreInit();

            System.Numerics.Vector2 v = new System.Numerics.Vector2();
            float vf = v.Y;
        }

        private static async void CoreInit()
        {
            await LogManager.InitializeAsync();
            await ShoutcastStationMediaPlayer.InitializeAsync();

            LogManager.Info(typeof(App), "CoreInitialization Complete");
        }

        private async void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            LogManager.Log(typeof(App), "BEGIN Unhandled Exception");
            LogManager.Error(typeof(App), e.Exception.ToString());
            LogManager.Error(typeof(App), e.Exception.StackTrace);

            if (e.Exception.InnerException != null)
                LogManager.Error(typeof(App), e.Exception.InnerException.ToString());

            LogManager.Error(typeof(App), e.Message);
            LogManager.Log(typeof(App), "END Unhandled Exception");

            await Task.Delay(50);

            //Application.Current.Exit();
        }

        public override async Task OnFreshLaunchAsync(LaunchActivatedEventArgs args)
        {
            //Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            LogManager.Info(typeof(App), "Application Launching");

            await Task.Delay(0);

            try
            {
                var payload = new ValueSet();
                payload.Add(Messages.AppLaunchOrResume, "");
                BackgroundMediaPlayer.SendMessageToBackground(payload);
            }
            catch (Exception)
            {

            }

            WindowManager.GetNavigationManagerForCurrentWindow()
                .RootNavigationService.NavigateTo<AppShellViewModel>();
        }

        public override async Task OnSuspendingAsync()
        {
            LogManager.Info(typeof(App), "Application Suspending");

            await base.OnSuspendingAsync();
        }

        public override async Task OnResumingAsync()
        {
            //await LogManager.InitializeAsync();

            LogManager.Info(typeof(App), "Application Resuming");

            await base.OnResumingAsync();
        }
    }
}
