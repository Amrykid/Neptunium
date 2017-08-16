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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxShellView : Page
    {
        private FrameNavigationService inlineNavigationService = null;
        public XboxShellView()
        {
            this.InitializeComponent();

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);

            NepApp.UI.SetOverlayParent(OverlayPanel);

            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();

            NowPlayingTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata)));
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadY)
            {
                if (TransportControlGrid.Visibility == Visibility.Collapsed)
                {
                    InlineFrame.IsEnabled = false;
                    TransportControlGrid.Visibility = Visibility.Visible;
                    PlayButton.Focus(FocusState.Programmatic);
                }
                else
                {
                    TransportControlGrid.Visibility = Visibility.Collapsed;
                    InlineFrame.IsEnabled = true;
                    InlineFrame.Focus(FocusState.Programmatic);
                }

                e.Handled = true;
            }
            
        }
    }
}
