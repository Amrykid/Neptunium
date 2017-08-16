using Crystal3.Navigation;
using Neptunium.Core.UI;
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
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Crystal3.UI.StatusManager.StatusManager;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel),
        NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class AppShellView : Page
    {
        private FrameNavigationService inlineNavigationService = null;
        public AppShellView()
        {
            this.InitializeComponent();

            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            NepApp.UI.SetOverlayParent(OverlayPanel);
            NepApp.UI.Overlay.RegisterDialogFragment<StationInfoDialogFragment, StationInfoDialog>();

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                //sets the mobile status bar to match the top app bar.
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = ((SolidColorBrush)topAppBar.Background)?.Color;
                statusBar.BackgroundOpacity = 1.0;
            }

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
                        topAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
                        bottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
                        break;
                    case Visibility.Visible:
                        topAppBar.IsEnabled = false;
                        bottomAppBar.IsEnabled = false;
                        topAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
                        bottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
                        break;
                }
            }));

            NowPlayingButton.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.Media, nameof(NepApp.Media.CurrentMetadata)));
            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;
            NepApp.Media.ConnectingBegin += Media_ConnectingBegin;
            NepApp.Media.ConnectingEnd += Media_ConnectingEnd;
            
            //            NowPlayingButton.RegisterPropertyChangedCallback(Button.DataContextProperty, new DependencyPropertyChangedCallback((btn, dp) =>
            //            {
            //#if DEBUG
            //                var x = btn.GetValue(Button.DataContextProperty);
            //                var y = x;
            //                System.Diagnostics.Debugger.Break();
            //#endif
            //            }));
        }

        private VisualStateChangedEventHandler noChromeHandler = null;
        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            if (inlineNavigationService.NavigationFrame.Content?.GetType().GetTypeInfo().GetCustomAttribute<NepAppUINoChromePageAttribute>() != null)
            {
                //no chrome mode

                Action noChrome = () =>
                {
                    topAppBar.Visibility = Visibility.Collapsed;
                    bottomAppBar.Visibility = Visibility.Collapsed;

                    RootSplitView.DisplayMode = SplitViewDisplayMode.Overlay;

                    RootSplitView.IsPaneOpen = false;
                };

                noChrome();

                noChromeHandler = new VisualStateChangedEventHandler((o, args) =>
                {
                    //this is to "fix" the splitview opening when extending the window in no chrome mode. it doesn't work very will
                    RootSplitView.Visibility = Visibility.Collapsed;
                    noChrome();
                    RootSplitView.Visibility = Visibility.Visible;
                });

                ShellVisualStateGroup.CurrentStateChanged += noChromeHandler;
                ShellVisualStateGroup.CurrentStateChanging += noChromeHandler;
            }
            else
            {
                //reactivate chrome

                topAppBar.Visibility = Visibility.Visible;
                bottomAppBar.Visibility = Visibility.Visible;

                //todo remember splitview state instead of trying to guess below.
                if (Window.Current.Bounds.Width >= 720)
                {
                    if (Window.Current.Bounds.Width >= 1080)
                    {
                        RootSplitView.DisplayMode = SplitViewDisplayMode.Inline;
                        RootSplitView.IsPaneOpen = true;
                    }
                    else
                    {
                        RootSplitView.DisplayMode = SplitViewDisplayMode.CompactInline;
                    }
                }

                if (noChromeHandler != null)
                {
                    ShellVisualStateGroup.CurrentStateChanged -= noChromeHandler;
                    ShellVisualStateGroup.CurrentStateChanging -= noChromeHandler;
                    noChromeHandler = null;
                }
            }
        }

        IndefiniteWorkStatusManagerControl statusControl = null;
        private void Media_ConnectingEnd(object sender, EventArgs e)
        {
            bottomAppBar.IsEnabled = true;

            statusControl?.Dispose();
        }

        private void Media_ConnectingBegin(object sender, EventArgs e)
        {
            bottomAppBar.IsEnabled = false;

            statusControl = WindowManager.GetStatusManagerForCurrentWindow().DoIndefiniteWork(null, "Connecting...");
        }

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
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
            if (NepApp.Media.CurrentStreamer != null) //only show the image if we're actually streaming something
            {
                NowPlayingButton.Height = double.NaN;
                NowPlayingImage.Visibility = Visibility.Visible;
            }
        }

        private void bottomAppBar_Closed(object sender, object e)
        {
            NowPlayingButton.Height = 45;
            NowPlayingImage.Visibility = Visibility.Collapsed;
        }

        private void NowPlayingButton_Click(object sender, RoutedEventArgs e)
        {
            //todo make a binding
            inlineNavigationService.SafeNavigateTo<NowPlayingPageViewModel>();
        }
    }
}
