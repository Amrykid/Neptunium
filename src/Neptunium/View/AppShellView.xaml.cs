using Crystal3;
using Crystal3.Navigation;
using Crystal3.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Neptunium.Core.UI;
using Neptunium.ViewModel;
using Neptunium.ViewModel.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls;
using static Crystal3.UI.StatusManager.StatusManager;
using Crystal3.Messaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel),
        NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class AppShellView : Page, Crystal3.Messaging.IMessagingTarget
    {
        private FrameNavigationService inlineNavigationService = null;
        private volatile bool isInNoChromeMode = false;
        public AppShellView()
        {
            this.InitializeComponent();

            CoreApplicationView view = CoreApplication.GetCurrentView();
            if (view != null)
                view.TitleBar.ExtendViewIntoTitleBar = false;

            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);
            App.RegisterUIDialogs();
            SetTitleBarAndMobileStatusBarToMatchAppBar();

            PageTitleBlock.SetBinding(TextBlock.TextProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));
            //PageTitleBlock.SetValue(TextBlock.TextProperty, NepApp.UI.ViewTitle);

            Window.Current.SizeChanged += Current_SizeChanged;

            NowPlayingButton.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;
            NepApp.MediaPlayer.ConnectingBegin += Media_ConnectingBegin;
            NepApp.MediaPlayer.ConnectingEnd += Media_ConnectingEnd;
            NepApp.MediaPlayer.IsCastingChanged += Media_IsCastingChanged;
            NepApp.MediaPlayer.MediaEngagementChanged += MediaPlayer_MediaEngagementChanged;

            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;

            Messenger.AddTarget(this);
        }

        private void MediaPlayer_MediaEngagementChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                bottomAppBar.Visibility = NepApp.MediaPlayer.IsMediaEngaged ? Visibility.Visible : Visibility.Collapsed;

                if (NepApp.MediaPlayer.IsMediaEngaged)
                {
                    NepApp.SongManager.RefreshMetadata();
                }
            });
        }

        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            WindowManager.GetWindowServiceForCurrentWindow().SetAppViewBackButtonVisibility(true);
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            WindowManager.GetWindowServiceForCurrentWindow().SetAppViewBackButtonVisibility(inlineNavigationService.CanGoBackward);

            bottomAppBar.Visibility = NepApp.MediaPlayer.IsMediaEngaged ? Visibility.Visible : Visibility.Collapsed;
            topAppBar.Visibility = Visibility.Visible;
        }

        private void Media_IsCastingChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                UpdateCastingUI();

                if (!isInNoChromeMode)
                    SetTitleBarAndMobileStatusBarToMatchAppBar();
            });
        }

        private void UpdateCastingUI()
        {
            if (NepApp.MediaPlayer.IsCasting)
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                topAppBar.Background = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.AccentDark3));
                bottomAppBar.Background = topAppBar.Background;
            }
            else
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                topAppBar.Background = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.Accent));
                bottomAppBar.Background = topAppBar.Background;
            }
        }

        private void SetTitleBarAndMobileStatusBarToMatchAppBar()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                //sets the mobile status bar to match the top app bar.
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = ((SolidColorBrush)topAppBar.Background)?.Color;
                statusBar.BackgroundOpacity = 1.0;
            }


            ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = ((SolidColorBrush)topAppBar.Background)?.Color;
            ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = ((SolidColorBrush)topAppBar.Background)?.Color;
        }

        private void SetMobileStatusBarToTransparent()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                //sets the mobile status bar to match the top app bar.
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = Colors.Transparent;
                statusBar.BackgroundOpacity = 0.0;
            }

            ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = Colors.Transparent;
            ApplicationView.GetForCurrentView().TitleBar.ButtonBackgroundColor = Colors.Transparent;

        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            CollapseBottomAppBarBasedOnSize();
        }

        private void CollapseBottomAppBarBasedOnSize()
        {
            if (NepApp.UI.Overlay.IsOverlayedDialogVisible)
            {
                //only collapse the app bars on smaller screens.
                if (Window.Current.Bounds.Width < 720)
                {
                    topAppBar.Visibility = Visibility.Collapsed;
                    bottomAppBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    topAppBar.Visibility = Visibility.Visible;
                    bottomAppBar.Visibility = NepApp.MediaPlayer.IsMediaEngaged ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            WindowManager.GetWindowServiceForCurrentWindow().SetAppViewBackButtonVisibility(inlineNavigationService.CanGoBackward);

            if (inlineNavigationService.NavigationFrame.Content?.GetType().GetTypeInfo().GetCustomAttribute<NepAppUINoChromePageAttribute>() != null)
            {
                //no chrome mode

                topAppBar.Visibility = Visibility.Collapsed;
                bottomAppBar.Visibility = Visibility.Collapsed;

                RootSplitView.IsPaneOpen = false;
                RootSplitView.DisplayMode = SplitViewDisplayMode.Overlay;

                SetMobileStatusBarToTransparent();

                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
                    RootGrid.Margin = new Thickness(0, -25, 0, 0);
                }


                ShellVisualStateGroup.States.Remove(DesktopVisualState);
                ShellVisualStateGroup.States.Remove(TabletVisualState);
                ShellVisualStateGroup.States.Remove(PhoneVisualState);

                isInNoChromeMode = true;
            }
            else
            {
                //reactivate chrome

                topAppBar.Visibility = Visibility.Visible;
                bottomAppBar.Visibility = NepApp.MediaPlayer.IsMediaEngaged ? Visibility.Visible : Visibility.Collapsed;

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

                UpdateCastingUI();

                SetTitleBarAndMobileStatusBarToMatchAppBar();

                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

                if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                {
                    RootGrid.Margin = new Thickness(0);
                }

                if (!ShellVisualStateGroup.States.Contains(DesktopVisualState))
                    ShellVisualStateGroup.States.Add(DesktopVisualState);

                if (!ShellVisualStateGroup.States.Contains(TabletVisualState))
                    ShellVisualStateGroup.States.Add(TabletVisualState);

                if (!ShellVisualStateGroup.States.Contains(PhoneVisualState))
                    ShellVisualStateGroup.States.Add(PhoneVisualState);

                isInNoChromeMode = false;
            }
        }

        IndefiniteWorkStatusManagerControl statusControl = null;
        private void Media_ConnectingEnd(object sender, EventArgs e)
        {
            statusControl?.Dispose();

            if (NepApp.MediaPlayer.CurrentStream != null)
            {
                NepApp.UI.Overlay.ShowSnackBarMessageAsync("Now Streaming - " + NepApp.MediaPlayer.CurrentStream.ParentStation.Name);
            }
        }

        private void Media_ConnectingBegin(object sender, EventArgs e)
        {
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

                //AppBarButton doesn't seem to like the ManualRelayCommand so, I have to set its IsEnabled property here.
                sleepTimerBtn.Visibility = e.IsPlaying ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            NepApp.UI.Notifier.VibrateClick();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            sleepTimerBtn.Visibility = NepApp.MediaPlayer.IsPlaying ? Visibility.Visible : Visibility.Collapsed;

            //FeedbackButton.Visibility = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {
            NepApp.UI.Notifier.VibrateClick();
            RootSplitView.IsPaneOpen = true;
        }

        private void TogglePaneButton_Unchecked(object sender, RoutedEventArgs e)
        {
            NepApp.UI.Notifier.VibrateClick();
            RootSplitView.IsPaneOpen = false;
        }

        private void NowPlayingButton_Click(object sender, RoutedEventArgs e)
        {
            NepApp.UI.Notifier.VibrateClick();

            //todo make a binding
            inlineNavigationService.SafeNavigateTo<NowPlayingPageViewModel>();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            NepApp.UI.Notifier.VibrateClick();

            //dismiss the menu if its open.
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                TogglePaneButton.IsChecked = false;
        }

        private void HandoffListButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ListItemButton;

            if (btn.DataContext == null) return;

            NepApp.UI.Notifier.VibrateClick();

            this.GetViewModel<AppShellViewModel>()
                .HandoffFragment
                .HandOffCommand
                .Execute(btn.DataContext);
        }

        public void OnReceivedMessage(Message message, Action<object> resultCallback)
        {
            switch (message.Name)
            {
                case "ShowHandoffFlyout":
                    App.Dispatcher.RunAsync(() =>
                    {
                        HandoffButton.Flyout.ShowAt(HandoffButton);
                    });

                    break;
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return new string[] { "ShowHandoffFlyout" };
        }
    }
}
