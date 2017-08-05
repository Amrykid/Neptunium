using Crystal3.Navigation;
using Neptunium.ViewModel;
using Neptunium.ViewModel.Dialog;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel))]
    public sealed partial class AppShellView : Page
    {
        public AppShellView()
        {
            this.InitializeComponent();

            Binding navItemBinding = NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems));

            //todo figure out a way to update the selected radio item.
            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, navItemBinding);

            NepApp.UI.SetNavigationService(WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two));

            NepApp.UI.SetOverlayParent(OverlayPanel);

            PageTitleBlock.SetBinding(TextBlock.TextProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));
            //PageTitleBlock.SetValue(TextBlock.TextProperty, NepApp.UI.ViewTitle);

            OverlayPanel.RegisterPropertyChangedCallback(Grid.VisibilityProperty, new DependencyPropertyChangedCallback((grid, p) =>
            {
                Visibility property = (Visibility)grid.GetValue(Grid.VisibilityProperty);
                switch (property)
                {
                    case Visibility.Collapsed:
                        topAppBar.IsEnabled = true;
                        bottomAppBar.IsEnabled = true;
                        break;
                    case Visibility.Visible:
                        topAppBar.IsEnabled = false;
                        bottomAppBar.IsEnabled = false;
                        break;
                }
            }));

            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();

            Binding nowPlayingBinding = NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata));

            NowPlayingButton.SetBinding(Button.DataContextProperty, nowPlayingBinding);
//            NowPlayingButton.RegisterPropertyChangedCallback(Button.DataContextProperty, new DependencyPropertyChangedCallback((btn, dp) =>
//            {
//#if DEBUG
//                var x = btn.GetValue(Button.DataContextProperty);
//                var y = x;
//                System.Diagnostics.Debugger.Break();
//#endif
//            }));
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = true;
        }

        private void TogglePaneButton_Unchecked(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = false;
        }

        private void bottomAppBar_Opened(object sender, object e)
        {
            NowPlayingButton.Height = double.NaN;
            NowPlayingImage.Visibility = Visibility.Visible;
        }

        private void bottomAppBar_Closed(object sender, object e)
        {
            NowPlayingButton.Height = 45;
            NowPlayingImage.Visibility = Visibility.Collapsed;
        }
    }
}
