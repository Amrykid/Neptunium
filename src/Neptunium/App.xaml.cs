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
            ShoutcastStationMediaPlayer.InitializeAsync();
        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();

            //Debugger.Break();
        }

        public override void OnFreshLaunch(LaunchActivatedEventArgs args)
        {
            //Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            var payload = new ValueSet();
            payload.Add(Messages.AppLaunchOrResume, "");
            BackgroundMediaPlayer.SendMessageToBackground(payload);

            WindowManager.GetNavigationManagerForCurrentWindow().RootNavigationService.NavigateTo<AppShellViewModel>();
        }
    }
}
