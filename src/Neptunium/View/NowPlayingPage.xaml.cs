using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
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
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.NowPlayingPageViewModel),
        Crystal3.Navigation.NavigationViewSupportedPlatform.Desktop | Crystal3.Navigation.NavigationViewSupportedPlatform.Mobile)]
    [Neptunium.Core.UI.NepAppUINoChromePage()]
    public sealed partial class NowPlayingPage : Page
    {
        public NowPlayingPage()
        {
            this.InitializeComponent();

            ShellVisualStateGroup.CurrentStateChanged += ShellVisualStateGroup_CurrentStateChanged;
            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;

            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                //https://blogs.msdn.microsoft.com/universal-windows-app-model/2017/02/11/compactoverlay-mode-aka-picture-in-picture/
                compactViewButton.Visibility = Visibility.Visible;

                compactViewButton.IsChecked = ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay;
                compactViewButton.Checked += compactViewButton_Checked;
                compactViewButton.Unchecked += compactViewButton_Unchecked;
            }
        }

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdatePlaybackStatus(e.IsPlaying);
            });
        }

        private void UpdatePlaybackStatus(bool isPlaying)
        {
            if (isPlaying)
            {
                playPauseButton.Label = "Pause";
                playPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).PausePlaybackCommand;
            }
            else
            {
                playPauseButton.Label = "Play";
                playPauseButton.Icon = new SymbolIcon(Symbol.Play);
                playPauseButton.Command = ((NowPlayingPageViewModel)this.DataContext).ResumePlaybackCommand;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    //prevent navigation until we're switched out of compact overlay mode.
                    e.Cancel = true;
                    return;
                }

                compactViewButton.Checked -= compactViewButton_Checked;
                compactViewButton.Unchecked -= compactViewButton_Unchecked;
            }

            NepApp.Media.IsPlayingChanged -= Media_IsPlayingChanged;
            ShellVisualStateGroup.CurrentStateChanged -= ShellVisualStateGroup_CurrentStateChanged;

            base.OnNavigatingFrom(e);
        }

        private VisualState lastVisualState = null;
        private bool isInCompactMode = false;

        private async void compactViewButton_Checked(object sender, RoutedEventArgs e)
        {
            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            compactOptions.CustomSize = new Windows.Foundation.Size(320, 280);

            bool modeSwitched = await ApplicationView.GetForCurrentView()
                .TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);

            if (modeSwitched)
            {
                if (ShellVisualStateGroup.States.Contains(TabletVisualState))
                {
                    ShellVisualStateGroup.States.Remove(TabletVisualState);
                    ShellVisualStateGroup.States.Remove(DesktopVisualState);
                    ShellVisualStateGroup.States.Remove(PhoneVisualState);
                }

                isInCompactMode = true;
                ShellVisualStateGroup.CurrentStateChanged -= ShellVisualStateGroup_CurrentStateChanged;
                VisualStateManager.GoToState(this, CompactVisualState.Name, true);
            }
        }

        private async void compactViewButton_Unchecked(object sender, RoutedEventArgs e)
        {
            bool modeSwitched = await ApplicationView.GetForCurrentView()
                .TryEnterViewModeAsync(ApplicationViewMode.Default,
                    ViewModePreferences.CreateDefault(ApplicationViewMode.Default));

            if (modeSwitched)
            {
                if (!ShellVisualStateGroup.States.Contains(TabletVisualState))
                {
                    ShellVisualStateGroup.States.Add(TabletVisualState);
                    ShellVisualStateGroup.States.Add(DesktopVisualState);
                    ShellVisualStateGroup.States.Add(PhoneVisualState);
                }

                isInCompactMode = false;

                if (lastVisualState != null)
                    VisualStateManager.GoToState(this, lastVisualState.Name, true);

                ShellVisualStateGroup.CurrentStateChanged += ShellVisualStateGroup_CurrentStateChanged;
            }
        }

        private void ShellVisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState != CompactVisualState)
            {
                lastVisualState = e.NewState;
            }
        }

        private void ShellVisualStateGroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (isInCompactMode)
            {
                e.NewState = CompactVisualState;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePlaybackStatus(NepApp.Media.IsPlaying);
        }
    }
}
