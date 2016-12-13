using Crystal3.Model;
using Neptunium.Data.Stations;
using Neptunium.Fragments;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Neptunium.View.Fragments
{
    public sealed partial class NowPlayingInfoBar : UserControl
    {
        public NowPlayingInfoBar()
        {
            this.InitializeComponent();

            App.Current.Resuming += Current_Resuming;
            StationMediaPlayer.IsPlayingChanged += StationMediaPlayer_IsPlayingChanged;
            StationMediaPlayer.ConnectingStatusChanged += StationMediaPlayer_ConnectingStatusChanged;

            this.RegisterPropertyChangedCallback(DataContextProperty, HandleNowPlayingInfoContextChanged);

            PART_GlassPane.Animate = false;

            var accentColor = (Color)this.Resources["SystemAccentColor"];
            PART_GlassPane.ChangeBlurColor(accentColor);

            PART_GlassPane.Animate = true;

            RefreshMediaButtons();
        }

        private void StationMediaPlayer_ConnectingStatusChanged(object sender, StationMediaPlayerConnectingStatusChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                this.IsEnabled = !e.IsConnecting;
            });
        }

        private void StationMediaPlayer_IsPlayingChanged(object sender, EventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                RefreshMediaButtons();
            });
        }

        private void Current_Resuming(object sender, object e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                RefreshMediaButtons();
            });
        }

        private NowPlayingViewFragment lastNowPlayingFrag = null;
        private void HandleNowPlayingInfoContextChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (lastNowPlayingFrag != null)
            {
                lastNowPlayingFrag.PropertyChanged -= NowPlayingFrag_PropertyChanged;
            }

            var newFrag = this.GetValue(dp) as NowPlayingViewFragment;

            if (newFrag != null)
            {
                newFrag.PropertyChanged += NowPlayingFrag_PropertyChanged;
                lastNowPlayingFrag = newFrag;
            }
        }


        private async void NowPlayingFrag_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentStation")
            {
                NowPlayingViewFragment fragment = sender as NowPlayingViewFragment;
                if (fragment.CurrentStation != null)
                {
                    //todo make this a setting
                    var color = await StationSupplementaryDataManager.GetStationLogoDominantColorAsync(fragment.CurrentStation);
                    PART_GlassPane.ChangeBlurColor(color);
                }
            }
        }

        public static readonly DependencyProperty NowPlayingInfoContextProperty = DependencyProperty.Register("NowPlayingInfoContext",
            typeof(NowPlayingViewFragment), typeof(NowPlayingInfoBar), new PropertyMetadata(null));


        public NowPlayingViewFragment NowPlayingInfoContext
        {
            get { return (NowPlayingViewFragment)GetValue(NowPlayingInfoContextProperty); }
            set { SetValue(NowPlayingInfoContextProperty, value); }
        }

        public static readonly DependencyProperty SleepTimerContextProperty = DependencyProperty.Register("SleepTimerContext",
            typeof(SleepTimerFlyoutViewFragment), typeof(NowPlayingInfoBar), new PropertyMetadata(null));

        public SleepTimerFlyoutViewFragment SleepTimerContext
        {
            get { return (SleepTimerFlyoutViewFragment)GetValue(SleepTimerContextProperty); }
            set { SetValue(SleepTimerContextProperty, value); }
        }

        internal void ShowHandoffFlyout()
        {
            lowerAppBarHandOffButton.Flyout.ShowAt(this);
        }

        public static readonly DependencyProperty HandOffViewFragmentProperty = DependencyProperty.Register("HandOffViewFragment",
            typeof(HandOffFlyoutViewFragment), typeof(NowPlayingInfoBar), new PropertyMetadata(null));

        public HandOffFlyoutViewFragment HandOffViewFragment
        {
            get { return (HandOffFlyoutViewFragment)GetValue(HandOffViewFragmentProperty); }
            set { SetValue(HandOffViewFragmentProperty, value); }
        }

        public static readonly DependencyProperty NowPlayingInfoButtonCommandProperty = DependencyProperty.Register("NowPlayingInfoButtonCommand",
            typeof(ICommand), typeof(NowPlayingInfoBar), new PropertyMetadata(null));

        public ICommand NowPlayingInfoButtonCommand
        {
            get { return (ICommand)GetValue(NowPlayingInfoButtonCommandProperty); }
            set { SetValue(NowPlayingInfoButtonCommandProperty, value); }
        }

        public void RefreshMediaButtonsFromMediaPlayer(MediaPlayer sender)
        {
            if (this.DataContext != null)
            {
                var fragment = this.DataContext as NowPlayingViewFragment;

                if (fragment != null)
                {
                    switch (sender.PlaybackSession.PlaybackState)
                    {
                        case MediaPlaybackState.Playing:
                        case MediaPlaybackState.Opening:
                        case MediaPlaybackState.Buffering:
                            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                            PlayPauseButton.SetValue(ToolTipService.ToolTipProperty, "Pause");
                            PlayPauseButton.Command = fragment.PauseCommand;
                            break;
                        case MediaPlaybackState.Paused:
                        case MediaPlaybackState.None:
                        default:
                            PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
                            PlayPauseButton.SetValue(ToolTipService.ToolTipProperty, "Play");
                            PlayPauseButton.Command = fragment.PlayCommand;
                            break;
                    }
                }

            }
        }

        public void RefreshMediaButtons()
        {
            if (this.DataContext != null)
            {
                var fragment = this.DataContext as NowPlayingViewFragment;

                if (fragment != null)
                {
                    if (StationMediaPlayer.IsPlaying)
                    {
                        PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                        PlayPauseButton.SetValue(ToolTipService.ToolTipProperty, "Pause");
                        PlayPauseButton.Command = fragment.PauseCommand;
                    }
                    else
                    {
                        PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
                        PlayPauseButton.SetValue(ToolTipService.ToolTipProperty, "Play");
                        PlayPauseButton.Command = fragment.PlayCommand;
                    }
                }

            }
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

        private void VisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {

        }
    }
}
