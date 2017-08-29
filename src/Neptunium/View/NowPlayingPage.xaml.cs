using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.NowPlayingPageViewModel),
        Crystal3.Navigation.NavigationViewSupportedPlatform.Desktop | Crystal3.Navigation.NavigationViewSupportedPlatform.Mobile)]
    [Neptunium.Core.UI.NepAppUINoChromePage()]
    public sealed partial class NowPlayingPage : Page
    {
        public NowPlayingPage()
        {
            this.InitializeComponent();

            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                //https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/
                compactViewButton.Visibility = Visibility.Visible;

                compactViewButton.IsChecked = ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay;
                compactViewButton.Checked += compactViewButton_Checked;
                compactViewButton.Unchecked += compactViewButton_Unchecked;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    //prevent navigation until we're switched out of compact overlay mode.
                    e.Cancel = true;
                    return;
                }

                compactViewButton.Checked -= compactViewButton_Checked;
                compactViewButton.Unchecked -= compactViewButton_Unchecked;
            }

            base.OnNavigatingFrom(e);
        }

        private async void compactViewButton_Checked(object sender, RoutedEventArgs e)
        {
            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            compactOptions.CustomSize = new Windows.Foundation.Size(320, 200);

            bool modeSwitched = await ApplicationView.GetForCurrentView()
                .TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
        }

        private async void compactViewButton_Unchecked(object sender, RoutedEventArgs e)
        {
            bool modeSwitched = await ApplicationView.GetForCurrentView()
                .TryEnterViewModeAsync(ApplicationViewMode.Default, 
                    ViewModePreferences.CreateDefault(ApplicationViewMode.Default));
        }
    }
}
