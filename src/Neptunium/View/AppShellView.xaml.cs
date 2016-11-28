using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Fragments;
using Neptunium.Logging;
using Neptunium.Media;
using Neptunium.MediaSourceStream;
using Neptunium.Services.SnackBar;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel), NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile | NavigationViewSupportedPlatform.Xbox)]
    public partial class AppShellView : Page
    {
        private NavigationService inlineNavService = null;
        public AppShellView()
        {
            LogManager.Info(typeof(AppShellView), "AppShellView ctor");

            this.InitializeComponent();

            inlineNavService = WindowManager.GetNavigationManagerForCurrentWindow()
                .RegisterFrameAsNavigationService(inlineFrame, FrameLevel.Two);
            inlineNavService.NavigationServicePreNavigatedSignaled += AppShellView_NavigationServicePreNavigatedSignaled;

            this.Loaded += AppShellView_Loaded;
            this.Unloaded += AppShellView_Unloaded;
            this.KeyDown += AppShellView_KeyDown;

            this.DataContextChanged += AppShellView_DataContextChanged;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                IoC.Current.Register<ISnackBarService>(new SnackBarService(xboxSnackBarGrid));

#if DEBUG
                if (Crystal3.CrystalApplication.GetCurrentAsCrystalApplication().Options.OverridePlatformDetection)
                    VisualStateManager.GoToState(this, XboxVisualState.Name, true);
#endif
            }
            else
            {
                IoC.Current.Register<ISnackBarService>(new SnackBarService(snackBarGrid));
            }

        }

        private void AppShellView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.GamepadView:
                        RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen; //when a controller presses the view button, toggle the splitview.

                        if (RootSplitView.IsPaneOpen)
                        {
                            var currentNavItem = ((RadioButton)RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton).FirstOrDefault(x => (bool)((RadioButton)x).IsChecked));

                            if (currentNavItem != null)
                                currentNavItem.Focus(FocusState.Programmatic);
                        }
                        break;
                    case Windows.System.VirtualKey.GamepadY:
                        if (StationMediaPlayer.IsPlaying)
                            lowerAppBarHandOffButton.Flyout.ShowAt(lowerAppBar);
                        break;
                    case Windows.System.VirtualKey.GamepadX:
                        {
                            //mimic's groove music uwp on xbox one

                            if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                                inlineNavService.NavigateTo<NowPlayingViewViewModel>();
                            else if (inlineNavService.CanGoBackward)
                                inlineNavService.GoBack();
                        }
                        break;

                }
            }
        }

        private void AppShellView_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void AppShellView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            LogManager.Info(typeof(AppShellView), "DataContextChanged: " + (args.NewValue == null ? "null" : args.NewValue.GetType().FullName));
        }

        private async void Current_Resuming(object sender, object e)
        {
            await Task.Delay(500);

            await App.Dispatcher.RunAsync(Crystal3.Core.IUIDispatcherPriority.Low, () =>
            {
                RefreshMediaButtons(BackgroundMediaPlayer.Current);
            });
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            App.Dispatcher.RunAsync(Crystal3.Core.IUIDispatcherPriority.Low, () =>
            {
                RefreshMediaButtons(sender);
            });

        }

        private void RefreshMediaButtons(MediaPlayer sender)
        {
            if (this.DataContext != null)
            {
                //var upperBarBtn = upperAppBar.PrimaryCommands.First(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                var lowerBarBtn = lowerAppBar.PrimaryCommands.FirstOrDefault(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                if (lowerBarBtn == null)
                {
                    //its possible the command got pushed to secondary commands
                    lowerBarBtn = lowerAppBar.SecondaryCommands.FirstOrDefault(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                }


                switch (sender.PlaybackSession.PlaybackState)
                {
                    case MediaPlaybackState.Playing:
                    case MediaPlaybackState.Opening:
                    case MediaPlaybackState.Buffering:
                        lowerBarBtn.Icon = new SymbolIcon(Symbol.Pause);
                        lowerBarBtn.Label = "Pause";
                        lowerBarBtn.Command = (this.DataContext as AppShellViewModel).PauseCommand;
                        break;
                    case MediaPlaybackState.Paused:
                    case MediaPlaybackState.None:
                    default:
                        lowerBarBtn.Icon = new SymbolIcon(Symbol.Play);
                        lowerBarBtn.Label = "Play";
                        lowerBarBtn.Command = (this.DataContext as AppShellViewModel).PlayCommand;
                        break;
                }

            }
        }

        private void AppShellView_NavigationServicePreNavigatedSignaled(object sender, NavigationServicePreNavigatedSignaledEventArgs e)
        {
            LogManager.Info(typeof(AppShellView), "AppShellView_NavigationServicePreNavigatedSignaled");

            RefreshNavigationSplitViewState(e.ViewModel);
        }

        private void AppShellView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewSize);
        }

        private void InlineFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        private void RefreshNavigationSplitViewState(ViewModelBase viewModelToGoTo)
        {
            //couldn't think of a clever way to do this
            App.Dispatcher.RunAsync(() =>
            {
                var viewModelType = viewModelToGoTo.GetType();
                if (viewModelType == typeof(StationsViewViewModel))
                {
                    stationsNavButton.IsChecked = true;
                }
                else if (viewModelType == typeof(SettingsViewViewModel))
                {
                    settingsNavButton.IsChecked = true;
                }
                else if (viewModelType == typeof(SongHistoryViewModel))
                {
                    songHistoryNavButton.IsChecked = true;
                }
                else if (viewModelType == typeof(NowPlayingViewViewModel))
                {
                    nowPlayingNavButton.IsChecked = true;
                }
                else
                {
                    foreach (RadioButton button in RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton))
                        button.IsChecked = false;

                    Debug.WriteLine("WARNING: Unimplemented navigation case - " + viewModelType.FullName);
                }

                string title = ((RadioButton)RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton).FirstOrDefault(x => (bool)((RadioButton)x).IsChecked))?.Content as string;
                if (string.IsNullOrWhiteSpace(title))
                {
                    if (viewModelType == typeof(StationInfoViewModel))
                    {
                        title = "Station Info";
                    }
                }

                CurrentPaneTitle.Text = title;
            });
        }

        private void AppShellView_Loaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged += AppShellView_SizeChanged;

            App.Current.Resuming += Current_Resuming;

            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            HandleUI();

            GoHome();

            FeedbackButton.Visibility = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported() ? Visibility.Visible : Visibility.Collapsed;

            foreach (RadioButton rb in RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton))
            {
                rb.Click += new RoutedEventHandler((sender2, e2) =>
                {
                    //dismiss the menu if its open.

                    //if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                    if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                        TogglePaneButton.IsChecked = false;
                });

            }

            App.Dispatcher.RunAsync(() =>
            {
                RefreshMediaButtons(BackgroundMediaPlayer.Current);
            });
        }

        private void HandleUI()
        {

        }

        private void GoHome()
        {
            WindowManager.GetNavigationManagerForCurrentWindow()
                            .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                            .NavigateTo<StationsViewViewModel>();

            inlineFrame.BackStack.Clear();

            inlineFrame.Navigated += InlineFrame_Navigated;
        }


        private void VisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            Debug.WriteLine("State Change: " + (e.OldState == null ? "null" : e.OldState.Name) + " -> " + e.NewState.Name);


        }

        private void HandOffDeviceFlyout_DeviceListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //todo turn this into a behavior.
            var listView = sender as ListView;
            var fragment = listView.DataContext as HandOffFlyoutViewFragment;

            fragment.Invoke(this.DataContext as ViewModelBase, e.ClickedItem);

            var parentGrid = listView.Parent as Grid;
            var parentFlyout = parentGrid.Parent as FlyoutPresenter;
            var parentPopup = parentFlyout.Parent as Popup;

            parentPopup.IsOpen = false;
        }

        private void nowPlayingAppBarGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                inlineNavService.NavigateTo<NowPlayingViewViewModel>();
        }

        private void SleepTimerItemsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //todo turn this into a behavior.
            var listView = sender as ListView;
            var fragment = listView.DataContext as SleepTimerFlyoutViewFragment;

            fragment.Invoke(this.DataContext as ViewModelBase, e.ClickedItem);

            var parentGrid = listView.Parent as Grid;
            var parentFlyout = parentGrid.Parent as FlyoutPresenter;
            var parentPopup = parentFlyout.Parent as Popup;

            parentPopup.IsOpen = false;
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }
    }
}
