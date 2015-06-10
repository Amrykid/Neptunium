﻿using Crystal3.Model;
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
        }

        private async void AppShellView_Loaded(object sender, RoutedEventArgs e)
        {
            GoHome();

            //BackgroundMediaPlayer.Current.AutoPlay = true;
            //await PlaySomething();
        }

        private void GoHome()
        {
            WindowManager.GetNavigationManagerForCurrentWindow()
                            .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                            .NavigateTo<StationsViewViewModel>();

            inlineFrame.BackStack.Clear();

            inlineFrame.Navigated += InlineFrame_Navigated;
        }

        private void Current_BufferingEnded(MediaPlayer sender, object args)
        {

        }

        private void Current_BufferingStarted(MediaPlayer sender, object args)
        {

        }

        private void Current_MediaOpened(MediaPlayer sender, object args)
        {

        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            Debug.WriteLine("BackgroundMediaPlayer.CurrentState: " + Enum.GetName(typeof(MediaPlayerState), sender.CurrentState));

            switch (sender.CurrentState)
            {
                default:
                    break;
            }
        }

        private void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {

        }

        private void Current_PlaybackMediaMarkerReached(MediaPlayer sender, PlaybackMediaMarkerReachedEventArgs args)
        {

        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void VisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            Debug.WriteLine("State Change: " + e.OldState.Name + " -> " + e.NewState.Name);
        }
    }
}
