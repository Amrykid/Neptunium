using Crystal3;
using Crystal3.Navigation;
using Neptunium.ViewModel;
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
    [Crystal3.Navigation.NavigationViewModel(typeof(CompactNowPlayingPageViewModel), Crystal3.Navigation.NavigationViewSupportedPlatform.Desktop | Crystal3.Navigation.NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class CompactNowPlayingPage : Page
    {
        public CompactNowPlayingPage()
        {
            this.InitializeComponent();
        }

        private async void compactViewButton_Click(object sender, RoutedEventArgs e)
        {
            //switch back to regular mode
            int mainWindowId = WindowManager.GetAllWindowServices().First().WindowViewId;
            bool modeSwitched = await ApplicationViewSwitcher.TryShowAsViewModeAsync(mainWindowId, ApplicationViewMode.Default,
                    ViewModePreferences.CreateDefault(ApplicationViewMode.Default));
        }
    }
}
