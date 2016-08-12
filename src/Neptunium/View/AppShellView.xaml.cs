using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Fragments;
using Neptunium.Logging;
using Neptunium.MediaSourceStream;
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

        }

        private void AppShellView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                switch(e.Key)
                {
                    case Windows.System.VirtualKey.GamepadView:
                        RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen; //when a controller presses the view button, toggle the splitview.
                        break;
                    case Windows.System.VirtualKey.GamepadY:
                        if (lowerAppBar.IsOpen)
                        {
                            lowerAppBar.IsOpen = false;
                            inlineFrame.Focus(FocusState.Programmatic);
                        }
                        else
                        {
                            lowerAppBar.IsOpen = true;

                            var lowerBarBtn = lowerAppBar.PrimaryCommands.FirstOrDefault(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                            if (lowerBarBtn == null)
                            {
                                lowerBarBtn.Focus(FocusState.Programmatic);
                            }
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
                var upperBarBtn = upperAppBar.PrimaryCommands.First(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                var lowerBarBtn = lowerAppBar.PrimaryCommands.FirstOrDefault(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                if (lowerBarBtn == null)
                {
                    //its possible the command got pushed to secondary commands
                    lowerBarBtn = lowerAppBar.SecondaryCommands.FirstOrDefault(x => (string)((FrameworkElement)x).Tag == "PlayPause") as AppBarButton;
                }

                foreach (var PlayPauseButton in new AppBarButton[]{ upperBarBtn, lowerBarBtn})
                {
                    switch (sender.PlaybackSession.PlaybackState)
                    {
                        case MediaPlaybackState.Playing:
                        case MediaPlaybackState.Opening:
                        case MediaPlaybackState.Buffering:
                            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                            PlayPauseButton.Content = "Pause";
                            PlayPauseButton.Command = (this.DataContext as AppShellViewModel).PauseCommand;
                            break;
                        case MediaPlaybackState.Paused:
                        case MediaPlaybackState.None:
                        default:
                            PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
                            PlayPauseButton.Content = "Play";
                            PlayPauseButton.Command = (this.DataContext as AppShellViewModel).PlayCommand;
                            break;
                    }
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
                    Debug.WriteLine("WARNING: Unimplemented navigation case - " + viewModelType.FullName);
                }

                CurrentPaneTitle.Text = ((RadioButton)RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton).First(x => (bool)((RadioButton)x).IsChecked)).Content as string;
            });
        }

        private void AppShellView_Loaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged += AppShellView_SizeChanged;

            App.Current.Resuming += Current_Resuming;

            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            HandleUI();

            GoHome();

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

#if DEBUG
            //NOTE: Keep commands in sync so that this code won't crash the app. Crashing the app/breaking the debugger helps me enforce that I should keep the commands the same.

            if ((lowerAppBar.PrimaryCommands.Count + lowerAppBar.SecondaryCommands.Count) != (upperAppBar.PrimaryCommands.Count + upperAppBar.SecondaryCommands.Count))
                if (Debugger.IsAttached)
                    Debugger.Break();
                else
                    throw new Exception();
#endif

            App.Dispatcher.RunAsync(() =>
            {
                RefreshMediaButtons(BackgroundMediaPlayer.Current);
            });

            //App.Dispatcher.RunAsync(() =>
            //{
            //    //https://channel9.msdn.com/Events/Build/2015/3-733
            //    if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            //    {
            //        //http://stackoverflow.com/questions/30262923/hiding-status-bar-white-bar-at-top-of-windows-10-universal-app-on-phone
            //        var statusBar = StatusBar.GetForCurrentView();
            //        statusBar.BackgroundColor = (Application.Current.Resources["AppThemeBrush"] as SolidColorBrush).Color;
            //        statusBar.ForegroundColor = Colors.White;
            //        statusBar.BackgroundOpacity = 1.0;
            //    }
            //});
        }

        private void HandleUI()
        {
            var stateGroup = VisualStateManager.GetVisualStateGroups(RootGrid).FirstOrDefault();

            //if (stateGroup != null)
            //{
            //    VisualState appropriateState = stateGroup.States.First(x =>
            //    {
            //        var trigger = x.StateTriggers.FirstOrDefault() as W;

            //        return trigger.MinWindowWidth <= Window.Current.Bounds.Width;
            //    });

            //    VisualStateManager.GoToState(this, appropriateState.Name, false);
            //}
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

        private void NowPlayingPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (this.DataContext as AppShellViewModel).GoToNowPlayingViewCommand.Execute(null);
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
    }
}
