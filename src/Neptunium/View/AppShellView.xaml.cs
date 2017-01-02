using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Fragments;
using Neptunium.Media;
using Neptunium.MediaSourceStream;
using Neptunium.Services.SnackBar;
using Neptunium.View.Fragments;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
            this.InitializeComponent();

            inlineNavService = WindowManager.GetNavigationManagerForCurrentWindow()
                .RegisterFrameAsNavigationService(inlineFrame, FrameLevel.Two);
            inlineNavService.NavigationServicePreNavigatedSignaled += AppShellView_NavigationServicePreNavigatedSignaled;

            this.Loaded += AppShellView_Loaded;
            this.Unloaded += AppShellView_Unloaded;
            this.KeyDown += AppShellView_KeyDown;

            this.DataContextChanged += AppShellView_DataContextChanged;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox || Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Desktop)
            {
                IoC.Current.Register<ISnackBarService>(new SnackBarService(floatingSnackBarGrid));
                if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                {
#if DEBUG
                    if (Crystal3.CrystalApplication.GetCurrentAsCrystalApplication().Options.OverridePlatformDetection)
                        VisualStateManager.GoToState(this, XboxVisualState.Name, true);
#endif
                }
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
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.GamepadY:
                        if (StationMediaPlayer.IsPlaying)
                            (lowerAppBar.Content as NowPlayingInfoBar)?.ShowHandoffFlyout();
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.GamepadX:
                        {
                            //mimic's groove music uwp on xbox one

                            if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                                inlineNavService.NavigateTo<NowPlayingViewViewModel>();
                            else if (inlineNavService.CanGoBackward)
                                inlineNavService.GoBack();

                            e.Handled = true;
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

        }

        private void AppShellView_NavigationServicePreNavigatedSignaled(object sender, NavigationServicePreNavigatedSignaledEventArgs e)
        {
            RefreshNavigationSplitViewState(e.ViewModel);
        }

        private void AppShellView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewSize);
        }

        private IDisposable nowPlayingIsMobileChangedObserver = null;
        private void InlineFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

            if ((inlineNavService.NavigationFrame.Content is NowPlayingView))
            {
                if (App.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                {
                    //lower app bar is always collapsed on xbox.
                    lowerAppBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    var nowPlayingPage = inlineNavService.NavigationFrame.Content as NowPlayingView;
                    nowPlayingIsMobileChangedObserver = Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => nowPlayingPage.IsMobileViewChanged += h,
                        h => nowPlayingPage.IsMobileViewChanged -= h)
                        .Subscribe(obv =>
                        {
                            lowerAppBar.Visibility = nowPlayingPage.IsMobileView ? Visibility.Collapsed : Visibility.Visible;
                        });
                }
            }
            else
            {
                if (App.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                {
                    //lower app bar is always collapsed on xbox.
                    lowerAppBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (nowPlayingIsMobileChangedObserver != null)
                    {
                        nowPlayingIsMobileChangedObserver.Dispose();
                        nowPlayingIsMobileChangedObserver = null;
                    }

                    lowerAppBar.Visibility = Visibility.Visible;
                }
            }
        }

        private void HandleLowerAppbarStateForNowPlayingPage(bool isOnNowPlayingPage)
        {
            if (App.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                //lower app bar is always collapsed on xbox.
                lowerAppBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (isOnNowPlayingPage)
                {
                    bool isMobileView = (inlineNavService.NavigationFrame.Content as NowPlayingView).IsMobileView;
                    if (isMobileView)
                    {
                        lowerAppBar.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        lowerAppBar.Visibility = Visibility.Visible;
                    }

                }
                else
                {
                    lowerAppBar.Visibility = Visibility.Visible;
                }

                //lowerAppBar.Visibility = Visibility.Visible;
            }
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
        }

        private void GoHome()
        {
            inlineNavService.NavigateTo<StationsViewViewModel>();

            inlineFrame.BackStack.Clear();

            inlineFrame.Navigated += InlineFrame_Navigated;
        }


        private void VisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
#if DEBUG
            Debug.WriteLine("State Change: " + (e.OldState == null ? "null" : e.OldState.Name) + " -> " + e.NewState.Name);
#endif
        }


        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }
    }
}
