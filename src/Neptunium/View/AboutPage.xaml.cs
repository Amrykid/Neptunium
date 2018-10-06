using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
    [Crystal3.Navigation.NavigationViewModel(typeof(AboutPageViewModel))]
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();

            VersionTextBlock.Text = "Version: " + string.Join(".", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
        }
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("http://github.com/Amrykid/Neptunium"));
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("https://www.microsoft.com/en-us/store/p/neptunium/9nblggh1r9cq"));
        }

        private void AppBarButton_Click_2(System.Object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("https://github.com/Amrykid/Neptunium/wiki/Privacy-Policy"));
        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}
