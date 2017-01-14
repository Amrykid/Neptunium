using Crystal3.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Neptunium.Data;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(StationsViewViewModel), NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class StationsView : Page
    {
        public StationsView()
        {
            this.InitializeComponent();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem;
        }

        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void ListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }

        private void ListItemButton_Click(object sender, RoutedEventArgs e)
        {
            ListItemButton button = (ListItemButton)sender;
            Grid innerGrid = button.Content as Grid;
            if (innerGrid != null)
            {
                var image = innerGrid.Children.FirstOrDefault(x => x is ImageEx);
                if (image != null)
                {
                    var service = ConnectedAnimationService.GetForCurrentView();

                    service.PrepareToAnimate("SelectedStationLogo", image);
                }
            }
        }
    }
}
