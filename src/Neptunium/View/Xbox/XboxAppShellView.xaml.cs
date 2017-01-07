using Crystal3.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Crystal3.Model;
using Neptunium.ViewModel;
using Neptunium.Services.SnackBar;
using Crystal3.InversionOfControl;
using System.Threading;
using Neptunium.Media;
using Neptunium.View.Fragments;
using Neptunium.ViewGlue;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View.Xbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxAppShellView : Page
    {
        private XboxAppShellViewPivotNavigationService inlineNavService = null;
        public XboxAppShellView()
        {
            this.InitializeComponent();

            IoC.Current.Register<ISnackBarService>(new SnackBarService(floatingSnackBarGrid));

            inlineNavService = new XboxAppShellViewPivotNavigationService(mainPivot, (Type auxViewModel) =>
            {
                if (auxViewModel == typeof(StationInfoViewModel))
                    return "Station Info";

                return null;
            });
            WindowManager.GetNavigationManagerForCurrentWindow()
                .RegisterCustomNavigationService(inlineNavService);
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            inlineNavService.NavigateTo<StationsViewViewModel>();

            inlineNavService.ClearBackStack();
        }


        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.GamepadY:
                        if (StationMediaPlayer.IsPlaying)
                            (lowerAppBar.Content as NowPlayingInfoBar)?.ShowHandoffFlyout();
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.GamepadX:
                        {
                            //mimic's groove music uwp on xbox one

                            if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                                inlineNavService.NavigateTo<NowPlayingViewViewModel>();
                            else if (inlineNavService.CanGoBackward)
                                inlineNavService.GoBack();

                            e.Handled = true;
                        }
                        break;

                }
            }
        }
    }
}
