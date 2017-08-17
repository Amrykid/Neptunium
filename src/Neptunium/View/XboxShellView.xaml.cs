using Crystal3.Navigation;
using Neptunium.Glue;
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

            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));
            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);

            NepApp.UI.SetOverlayParent(OverlayPanel);

            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();

            NowPlayingTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata)));

            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;
        }

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {
            //if (e.IsPlaying)
            //{
            //if (NepApp.Media.CurrentStreamer != null)
            //{
            //    App.Dispatcher.RunAsync(() =>
            //    {
            //        TransportControlsGridMediaPlayerElement.SetMediaPlayer(NepApp.Media.CurrentStreamer.Player);
            //    });
            //}
            //}

            App.Dispatcher.RunAsync(() =>
            {
                 if (e.IsPlaying)
                 {
                     PlayButton.Label = "Pause";
                     PlayButton.Icon = new SymbolIcon(Symbol.Pause);
                     PlayButton.Command = ((AppShellViewModel)this.DataContext).PausePlaybackCommand;
                 }
                 else
                 {
                     PlayButton.Label = "Play";
                     PlayButton.Icon = new SymbolIcon(Symbol.Play);
                     PlayButton.Command = ((AppShellViewModel)this.DataContext).ResumePlaybackCommand;
                 }
            });
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

                    if (InlineFrame.Content is IXboxInputPage)
                    {
                        ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
                    }

                    PlayButton.Focus(FocusState.Keyboard);
                }
                else
                {
                    TransportControlGrid.Visibility = Visibility.Collapsed;
                    InlineFrame.IsEnabled = true;

                    if (InlineFrame.Content is IXboxInputPage)
                    {
                        ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.GamepadMenu)
            {
                e.Handled = true;
                RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen;
            }
            
        }
    }
}
