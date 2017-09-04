using Crystal3.Navigation;
using Neptunium.Core.UI;
using Neptunium.Glue;
using Neptunium.ViewModel;
using Neptunium.ViewModel.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);

            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();

            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;

            NowPlayingTrackTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata)));
            NowPlayingArtistTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata)));

            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;
        }

        private bool isInNoChromeMode = false;
        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            if (inlineNavigationService.NavigationFrame.Content?.GetType().GetTypeInfo().GetCustomAttribute<NepAppUINoChromePageAttribute>() != null)
            {
                //no chrome mode
                RootSplitView.DisplayMode = SplitViewDisplayMode.Overlay;
                RootSplitView.IsPaneOpen = false;
                MediaGrid.Visibility = Visibility.Collapsed;
                isInNoChromeMode = true;
            }
            else
            {
                //reactivate chrome

                RootSplitView.DisplayMode = SplitViewDisplayMode.CompactInline;

                if (NepApp.Media.IsPlaying)
                    MediaGrid.Visibility = Visibility.Visible;

                isInNoChromeMode = false;

                if (InlineFrame.Content is IXboxInputPage)
                {
                    //makes sure that if we go left from the inline frame, we end up selecting the current page's item in the nav list.
                    IEnumerable<NepAppUINavigationItem> items = (IEnumerable<NepAppUINavigationItem>)SplitViewNavigationList.ItemsSource;
                    var selectedItem = items.First(x => x.IsSelected);
                    var container = SplitViewNavigationList.ContainerFromItem(selectedItem);
                    ((IXboxInputPage)InlineFrame.Content).SetLeftFocus((UIElement)container);
                }
            }
        }

        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
            }
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
            }
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

                    MediaGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    PlayButton.Label = "Play";
                    PlayButton.Icon = new SymbolIcon(Symbol.Play);
                    PlayButton.Command = ((AppShellViewModel)this.DataContext).ResumePlaybackCommand;

                    MediaGrid.Visibility = Visibility.Collapsed;
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
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                if (isInNoChromeMode) return;

                if (TransportControlGrid.Visibility == Visibility.Collapsed)
                {
                    InlineFrame.IsEnabled = false;

                    if (InlineFrame.Content is IXboxInputPage)
                    {
                        ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
                    }

                    TransportControlGrid.Visibility = Visibility.Visible;

                    PlayButton.Focus(FocusState.Keyboard);
                }
                else
                {
                    TransportControlGrid.Visibility = Visibility.Collapsed;
                    InlineFrame.IsEnabled = true;
                    InlineFrame.Focus(FocusState.Keyboard);

                    if (InlineFrame.Content is IXboxInputPage)
                    {
                        ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.GamepadMenu)
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                if (isInNoChromeMode) return;

                e.Handled = true;
                RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen;
            }

        }
    }
}
