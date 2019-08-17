using Crystal3.Navigation;
using Neptunium.Core.UI;
using Neptunium.Glue;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Crystal3.Messaging;
using WinRTXamlToolkit.Controls;
using Crystal3.UI;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
using Crystal3.UI.Commands;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxShellView : Page, Crystal3.Messaging.IMessagingTarget
    {
        private FrameNavigationService inlineNavigationService = null;
        private UISettings uiSettings = null;
        private XboxShellViewModelNowPlayingOverlayCoordinator nowPlayingOverlayCoordinator = null;
        public XboxShellView()
        {
            this.InitializeComponent();

            uiSettings = new Windows.UI.ViewManagement.UISettings();

            ///Set up navigation
            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentView().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;
            inlineNavigationService.PreBackRequested += InlineNavigationService_PreBackRequested;
            PageTitleTextBlock.SetBinding(TextBlock.TextProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));

            nowPlayingOverlayCoordinator = new XboxShellViewModelNowPlayingOverlayCoordinator(this);

            ///Set up dialogs
            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);
            App.RegisterUIDialogs();
            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;
            NepApp.UI.NoChromeStatusChanged += UI_NoChromeStatusChanged;

            ///Set up media metadata
            NowPlayingTrackTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NowPlayingArtistTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;
            //handler to allow the metadata to animate onto the screen for the first time.
            CurrentMediaMetadataPanel.SetBinding(Control.VisibilityProperty, NepApp.CreateBinding(NepApp.MediaPlayer, nameof(NepApp.MediaPlayer.IsMediaEngaged), binding =>
            {
                binding.Converter = new Crystal3.UI.Converters.BooleanToVisibilityConverter();
            }));

            ///Set up messaging
            Messenger.AddTarget(this);

            SetInlineFrameMarginPaddingForOverflowScrolling();

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.XamlCompositionBrushBase"))
            {
                //Add acrylic.

                Windows.UI.Xaml.Media.AcrylicBrush myBrush = new Windows.UI.Xaml.Media.AcrylicBrush();
                myBrush.BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.HostBackdrop;
                myBrush.TintColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush.FallbackColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush.Opacity = 0.6;
                myBrush.TintOpacity = 0.5;

                HeaderGrid.Background = myBrush;

                Windows.UI.Xaml.Media.AcrylicBrush myBrush2 = new Windows.UI.Xaml.Media.AcrylicBrush();
                myBrush2.BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop;
                myBrush2.TintColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush2.FallbackColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush2.Opacity = 0.6;
                myBrush2.TintOpacity = 0.5;

                TransportControlGrid.Background = myBrush;
                TransportControlGrid.BorderBrush = myBrush;
            }
            else
            {
                HeaderGrid.Background = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.Accent));
                TransportControlGrid.Background = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.Accent));
                TransportControlGrid.BorderBrush = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.Accent));
            }
        }

        private void UI_NoChromeStatusChanged(object sender, NepAppUIManagerNoChromeStatusChangedEventArgs e)
        {
            if (e.ShouldBeInNoChromeMode)
            {
                ActivateNoChromeMode();
            }
            else
            {
                DeactivateNoChromeMode();
            }
        }

        private void InlineNavigationService_PreBackRequested(object sender, NavigationManagerPreBackRequestedEventArgs e)
        {
            if (transportGridVisible)
            {
                e.Handled = true;
                HideTransportGrid();
            }
        }

        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {

        }

        private void DeactivateNoChromeMode()
        {
            //reactivate chrome

            HeaderGrid.Visibility = Visibility.Visible;
            SetInlineFrameMarginPaddingForOverflowScrolling();

            TransportControlGrid.Opacity = 0.1;
            transportGridVisible = false;

            SplitViewOpenButton.Focus(FocusState.Pointer); //reduces the amount of times that the splitview open button has the focus rectangle when that is used to open the menu.
        }

        private void SetInlineFrameMarginPaddingForOverflowScrolling()
        {
            InlineFrame.Margin = new Thickness(0, -100, 0, 0);
            InlineFrame.Padding = new Thickness(0, 200, 0, 0);
        }

        private void ActivateNoChromeMode()
        {
            //no chrome mode
            HeaderGrid.Visibility = Visibility.Collapsed;
            InlineFrame.Margin = new Thickness(0);
            InlineFrame.Padding = new Thickness(0);

            TransportControlGrid.Opacity = 0;
            transportGridVisible = false;
        }

        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            PreserveFocusFromInlineFramePage();
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            RestoreFocusToInlineFramePage();
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

                    HeaderControlHintIcon.Visibility = Visibility.Visible;
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

        private void HandleSplitViewPaneClose()
        {
            RestoreFocusToInlineFramePage();
        }

        private void RestoreFocusToInlineFramePage()
        {
            InlineFrame.Focus(FocusState.Programmatic);
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
            }
        }

        private void PreserveFocusFromInlineFramePage()
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
            }
        }

        private async void Page_KeyUp(object sender, KeyRoutedEventArgs e)
        {
#if DEBUG
            if (e.Key == Windows.System.VirtualKey.GamepadY || e.Key == Windows.System.VirtualKey.Y)
#else
            if (e.Key == Windows.System.VirtualKey.GamepadY)
#endif
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                if (NepApp.UI.IsInNoChromeMode) return;
                if (transportGridAnimating) return;

                if (!transportGridVisible)
                {
                    ShowTransportGrid();
                }
                else
                {
                    HideTransportGrid();
                }

                e.Handled = true;
            }
#if DEBUG
            else if (e.Key == Windows.System.VirtualKey.GamepadX || e.Key == Windows.System.VirtualKey.X)
#else
            else if (e.Key == Windows.System.VirtualKey.GamepadX)
#endif
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                if (transportGridAnimating) return;

                if (nowPlayingOverlayCoordinator.IsOverlayVisible())
                {
                    await nowPlayingOverlayCoordinator.HideOverlayAsync();
                }
                else
                {
                    await nowPlayingOverlayCoordinator.ShowOverlayAsync();
                }

            }
            else if (e.Key == Windows.System.VirtualKey.GamepadView || e.Key == Windows.System.VirtualKey.GamepadMenu)
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                //if (isInNoChromeMode) return;

                e.Handled = true;

                ShowNavigationFlyout();
            }
        }

        private volatile bool transportGridVisible = false;
        private volatile bool transportGridAnimating = false;
        private void ShowTransportGrid()
        {
            InlineFrame.IsEnabled = false;

            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
            }

            //TransportControlGrid.Visibility = Visibility.Visible;

            transportGridAnimating = true;

            Storyboard storyboard = ((Storyboard)TransportControlGrid.Resources["EnterStoryboard"]);

            EventHandler<object> handler = null;
            handler = new EventHandler<object>((x, y) =>
            {
                storyboard.Completed -= handler;
                transportGridAnimating = false;
                transportGridVisible = true;

                TransportControlGrid.IsHitTestVisible = true;

                PlayButton.Focus(FocusState.Keyboard);
                ElementSoundPlayer.Play(ElementSoundKind.Show);
            });

            storyboard.Completed += handler;
            storyboard.Begin();
        }

        private void HideTransportGrid()
        {
            transportGridAnimating = true;

            Storyboard storyboard = ((Storyboard)TransportControlGrid.Resources["ExitStoryboard"]);

            EventHandler<object> handler = null;
            handler = new EventHandler<object>((x, y) =>
            {
                storyboard.Completed -= handler;
                transportGridAnimating = false;
                transportGridVisible = false;

                TransportControlGrid.IsHitTestVisible = false;

                //TransportControlGrid.Visibility = Visibility.Collapsed;
                InlineFrame.IsEnabled = true;
                InlineFrame.Focus(FocusState.Programmatic);

                ElementSoundPlayer.Play(ElementSoundKind.Hide);

                if (InlineFrame.Content is IXboxInputPage)
                {
                    ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
                }
            });

            storyboard.Completed += handler;
            storyboard.Begin();
        }

        public void OnReceivedMessage(Message message, Action<object> resultCallback)
        {
            switch (message.Name)
            {
                case "ShowHandoffFlyout":
                    App.Dispatcher.RunWhenIdleAsync(() =>
                    {
                        this.GetViewModel<AppShellViewModel>()?.MediaCastingCommand.Execute(null);
                    });
                    break;
                case "ShowNowPlayingOverlay":
                    App.Dispatcher.RunWhenIdleAsync(async () =>
                    {
                        await nowPlayingOverlayCoordinator.ShowOverlayAsync();
                    });
                    break;
                case "HideNowPlayingOverlay":
                    App.Dispatcher.RunWhenIdleAsync(async () =>
                    {
                        await nowPlayingOverlayCoordinator.HideOverlayAsync();
                    });
                    break;
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return new string[] { "ShowHandoffFlyout", "ShowNowPlayingOverlay", "HideNowPlayingOverlay" };
        }

        private void SplitViewOpenButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNavigationFlyout();
        }

        private void ShowNavigationFlyout()
        {
            var navFlyout = ((MenuFlyout)LayoutRoot.ContextFlyout);
            var flyoutOptions = new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions()
            {
                Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full,
                ShowMode = Windows.UI.Xaml.Controls.Primitives.FlyoutShowMode.Standard
            };

            navFlyout.ShowAt(LayoutRoot, flyoutOptions);
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var navFlyout = sender as MenuFlyout;
            if (navFlyout == null) return;

            if (navFlyout.Items.Count > 0) return;

            foreach (var navItem in NepApp.UI.NavigationItems)
            {
                var menuItem = new MenuFlyoutItem();
                menuItem.Text = navItem.PageHeaderText;
                menuItem.Icon = navItem.Icon;
                menuItem.Command = new RelayCommand(param =>
                {
                    NepApp.UI.NavigateToItem(navItem, param);
                });
                navFlyout.Items.Add(menuItem);
            }
        }

        private void MenuFlyout_Closing(Windows.UI.Xaml.Controls.Primitives.FlyoutBase sender, Windows.UI.Xaml.Controls.Primitives.FlyoutBaseClosingEventArgs args)
        {
            //var navFlyout = sender as MenuFlyout;
            //navFlyout?.Items.Clear();
        }

        private class XboxShellViewModelNowPlayingOverlayCoordinator
        {
            private XboxShellView parentShell = null;
            private NavigationServiceBase navManager = null;
            private EventHandler<NavigationManagerPreBackRequestedEventArgs> backHandler = null;
            private SemaphoreSlim overlayLock = new SemaphoreSlim(1, 1);
            private volatile bool isOverlayAnimating = false;
            private XboxNowPlayingPage nowPlayingPage = null;
            internal XboxShellViewModelNowPlayingOverlayCoordinator(XboxShellView appShellView)
            {
                if (appShellView == null) throw new ArgumentNullException(nameof(appShellView));
                parentShell = appShellView;
                parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                navManager = WindowManager.GetNavigationManagerForCurrentView().GetNavigationServiceFromFrameLevel(FrameLevel.Two);

                backHandler = new EventHandler<NavigationManagerPreBackRequestedEventArgs>(async (o, e) =>
                {
                    if (!IsOverlayVisible())
                    {
                        //hack to handle the back button.
                        e.Handled = true;
                        await HideOverlayAsync();
                    }
                });

                nowPlayingPage = parentShell.NowPlayingPanel.Children[0] as XboxNowPlayingPage;
            }

            internal bool IsOverlayVisible()
            {
                return parentShell.NowPlayingPanel.Opacity > 0.0 && parentShell.NowPlayingPanel.IsHitTestVisible;
            }

            public async Task ShowOverlayAsync()
            {
                if (!IsOverlayVisible())
                {
                    if (isOverlayAnimating) return;

                    await overlayLock.WaitAsync();
                    isOverlayAnimating = true;
                    parentShell.NowPlayingPanel.Opacity = 0;
                    parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    await parentShell.NowPlayingPanel.Fade(.95f).StartAsync();
                    parentShell.NowPlayingPanel.IsHitTestVisible = true;
                    isOverlayAnimating = false;
                    navManager.PreBackRequested += backHandler;

                    parentShell.PreserveFocusFromInlineFramePage();

                    (nowPlayingPage as IXboxInputPage)?.FocusDefault();

                    //WindowManager.GetWindowServiceForCurrentView().SetAppViewBackButtonVisibility(true);
                    overlayLock.Release();
                }
            }

            public async Task HideOverlayAsync()
            {
                if (IsOverlayVisible())
                {
                    if (isOverlayAnimating) return;

                    await overlayLock.WaitAsync();
                    isOverlayAnimating = true;
                    await parentShell.NowPlayingPanel.Fade(0.0f).StartAsync();
                    parentShell.NowPlayingPanel.IsHitTestVisible = false;
                    parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    isOverlayAnimating = false;
                    navManager.PreBackRequested -= backHandler;

                    parentShell.RestoreFocusToInlineFramePage();

                    //WindowManager.GetWindowServiceForCurrentView().SetAppViewBackButtonVisibility(parentShell.inlineNavigationService.CanGoBackward);
                    overlayLock.Release();
                }
            }
        }
    }
}
