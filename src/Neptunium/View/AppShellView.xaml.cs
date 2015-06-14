using Crystal3.Model;
using Crystal3.Navigation;
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
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel))]
    public sealed partial class AppShellView : Page
    {
        public AppShellView()
        {
            this.InitializeComponent();

            this.Loaded += AppShellView_Loaded;

            WindowManager.GetNavigationManagerForCurrentWindow()
                .RegisterFrameAsNavigationService(inlineFrame, FrameLevel.Two);

            WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(FrameLevel.Two).NavigationServicePreNavigatedSignaled += AppShellView_NavigationServicePreNavigatedSignaled;

            this.SizeChanged += AppShellView_SizeChanged;

        }

        private void AppShellView_NavigationServicePreNavigatedSignaled(object sender, NavigationServicePreNavigatedSignaledEventArgs e)
        {
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
            var viewModelType = viewModelToGoTo.GetType();
            if (viewModelType == typeof(StationsViewViewModel))
            {
                stationsNavButton.IsChecked = true;
            }
            else if (viewModelType == typeof(ViewModel.NowPlayingViewViewModel))
            {
                nowPlayingNavButton.IsChecked = true;
            }
            else
            {
                Debug.WriteLine("WARNING: Unimplemented navigation case - " + viewModelType.FullName);
            }

            CurrentPaneTitle.Text = ((RadioButton)RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton).First(x => (bool)((RadioButton)x).IsChecked)).Content as string;
        }

        private void AppShellView_Loaded(object sender, RoutedEventArgs e)
        {
            GoHome();

            foreach (RadioButton rb in RootSplitViewPaneStackPanel.Children.Where(x => x is RadioButton))
            {
                rb.Click += new RoutedEventHandler((sender2, e2) =>
                {
                    //dismiss the menu if its open.

                    if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                        TogglePaneButton.IsChecked = false;
                });
            }
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
            Debug.WriteLine("State Change: " + e.OldState.Name + " -> " + e.NewState.Name);
        }
    }
}
