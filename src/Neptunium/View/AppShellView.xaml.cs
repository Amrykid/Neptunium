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
using Neptunium.Media;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel),
        NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile 
        | NavigationViewSupportedPlatform.Holographic | NavigationViewSupportedPlatform.Team)]
    public sealed partial class AppShellView : Page, Crystal3.Messaging.IMessagingTarget
    {
        private FrameNavigationService inlineNavigationService = null;
        private WindowService windowService = null;
        private ApplicationView applicationView = null;
        private CoreApplicationView coreApplicationView = null;
        private AppShellViewModelNowPlayingOverlayCoordinator nowPlayingOverlayCoordinator = null;
        private UISettings uiSettings = null;
        public AppShellView()
        {
            this.InitializeComponent();

            coreApplicationView = CoreApplication.GetCurrentView();
            //if (coreApplicationView != null)
            //    coreApplicationView.TitleBar.ExtendViewIntoTitleBar = true;

            applicationView = ApplicationView.GetForCurrentView();
            applicationView.TitleBar.BackgroundColor = Colors.Transparent;
            applicationView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            applicationView.TitleBar.InactiveBackgroundColor = Colors.Transparent;

            uiSettings = new Windows.UI.ViewManagement.UISettings();

            NavView.SetBinding(Microsoft.UI.Xaml.Controls.NavigationView.MenuItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentView().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            windowService = WindowManager.GetWindowServiceForCurrentView();
            UpdateSelectedNavigationItems();
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);
            App.RegisterUIDialogs();

            nowPlayingOverlayCoordinator = new AppShellViewModelNowPlayingOverlayCoordinator(this);


            NavView.SetBinding(Microsoft.UI.Xaml.Controls.NavigationView.HeaderProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));

            NowPlayingButton.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));

            NepApp.MediaPlayer.MediaEngagementChanged += MediaPlayer_MediaEngagementChanged;
            NepApp.MediaPlayer.IsPlayingChanged += MediaPlayer_IsPlayingChanged;


            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;

            Messenger.AddTarget(this);

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.XamlCompositionBrushBase"))
            {
                //Add acrylic.

                Windows.UI.Xaml.Media.AcrylicBrush myBrush = new Windows.UI.Xaml.Media.AcrylicBrush();
                myBrush.BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.HostBackdrop;
                myBrush.TintColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush.FallbackColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                myBrush.Opacity = 0.6;
                myBrush.TintOpacity = 0.5;

                bottomAppBar.Background = myBrush;
            }
            else
            {
                bottomAppBar.Background = new SolidColorBrush(uiSettings.GetColorValue(UIColorType.Accent));
            }
        }

        private void MediaPlayer_IsPlayingChanged(object sender, NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
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
                SleepTimerButton.IsEnabled = e.IsPlaying;
            });
        }

        private void MediaPlayer_MediaEngagementChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                bottomAppBar.Visibility = NepApp.MediaPlayer.IsMediaEngaged ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            windowService.SetAppViewBackButtonVisibility(true);

            foreach (var item in NepApp.UI.NavigationItems)
                item.IsEnabled = false;
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            windowService.SetAppViewBackButtonVisibility(inlineNavigationService.CanGoBackward);

            foreach (var item in NepApp.UI.NavigationItems)
                item.IsEnabled = true;
        }


        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            //windowService.SetAppViewBackButtonVisibility(inlineNavigationService.CanGoBackward);
            UpdateSelectedNavigationItems();
        }

        private void UpdateSelectedNavigationItems()
        {
            var viewModel = inlineNavigationService.GetNavigatedViewModel();
            if (viewModel == null) return;
            bool somethingSelected = false;
            foreach (var item in NepApp.UI.NavigationItems)
            {
                item.IsSelected = item.NavigationViewModelType == viewModel.GetType();
                if (item.IsSelected)
                {
                    NavView.SelectedItem = item;
                    somethingSelected = true;
                }
            }

            if (!somethingSelected)
            {
                NavView.SelectedItem = null;
            }
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

        private void NavView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {

            }
            else
            {
                var item = args.InvokedItemContainer as NepAppUINavigationItem;
                if (item != null)
                {
                    NepApp.UI.NavigateToItem(item);
                }
            }
        }

        private class AppShellViewModelNowPlayingOverlayCoordinator
        {
            private AppShellView parentShell = null;
            private NavigationServiceBase navManager = null;
            private EventHandler<NavigationManagerPreBackRequestedEventArgs> backHandler = null;
            internal AppShellViewModelNowPlayingOverlayCoordinator(AppShellView appShellView)
            {
                if (appShellView == null) throw new ArgumentNullException(nameof(appShellView));
                parentShell = appShellView;
                parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                navManager = WindowManager.GetNavigationManagerForCurrentView().GetNavigationServiceFromFrameLevel(FrameLevel.Two);
            }

            private bool IsOverlayVisible()
            {
                return parentShell.NowPlayingPanel.Opacity > 0.0 && parentShell.NowPlayingPanel.IsHitTestVisible;
            }

            public async Task ShowOverlayAsync()
            {
                if (!IsOverlayVisible())
                {
                    backHandler = new EventHandler<NavigationManagerPreBackRequestedEventArgs>(async (o, e) =>
                    {
                        //hack to handle the back button.
                        e.Handled = true;
                        navManager.PreBackRequested -= backHandler;
                        await HideOverlayAsync();
                    });
                    navManager.PreBackRequested += backHandler;

                    parentShell.NowPlayingPanel.Opacity = 0;
                    parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    await parentShell.NowPlayingPanel.Fade(.95f).StartAsync();
                    parentShell.NowPlayingPanel.IsHitTestVisible = true;
                    WindowManager.GetWindowServiceForCurrentView().SetAppViewBackButtonVisibility(true);
                }
            }

            public async Task HideOverlayAsync()
            {
                if (IsOverlayVisible())
                {
                    await parentShell.NowPlayingPanel.Fade(0.0f).StartAsync();
                    parentShell.NowPlayingPanel.IsHitTestVisible = false;
                    parentShell.NowPlayingPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    WindowManager.GetWindowServiceForCurrentView().SetAppViewBackButtonVisibility(parentShell.inlineNavigationService.CanGoBackward);
                }
            }
        }

        private void NowPlayingButton_Click(object sender, RoutedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(async () =>
            {
                await nowPlayingOverlayCoordinator.ShowOverlayAsync();
            });
        }
    }
}
