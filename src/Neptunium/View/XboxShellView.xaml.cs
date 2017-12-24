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
        public XboxShellView()
        {
            this.InitializeComponent();

            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));
            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            inlineNavigationService.PreBackRequested += InlineNavigationService_PreBackRequested;

            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);

            App.RegisterUIDialogs();

            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;

            NowPlayingTrackTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NowPlayingArtistTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));

            PageTitleTextBlock.SetBinding(TextBlock.TextProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));

            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;

            //handler to allow the metadata to animate onto the screen for the first time.
            CurrentMediaMetadataPanel.SetBinding(Control.VisibilityProperty, NepApp.CreateBinding(NepApp.MediaPlayer, nameof(NepApp.MediaPlayer.IsMediaEngaged), binding =>
            {
                binding.Converter = new Crystal3.UI.Converters.BooleanToVisibilityConverter();
            }));

            Messenger.AddTarget(this);
        }

        private void InlineNavigationService_PreBackRequested(object sender, NavigationManagerPreBackRequestedEventArgs e)
        {
            if (TransportControlGrid.Visibility == Visibility.Visible)
            {
                e.Handled = true;
                HideTransportGrid();
            }
        }

        private bool isInNoChromeMode = false;
        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            if (inlineNavigationService.NavigationFrame.Content?.GetType().GetTypeInfo().GetCustomAttribute<NepAppUINoChromePageAttribute>() != null)
            {
                //no chrome mode
                RootSplitView.IsPaneOpen = false;
                HeaderGrid.Visibility = Visibility.Collapsed;
                isInNoChromeMode = true;
            }
            else
            {
                //reactivate chrome

                HeaderGrid.Visibility = Visibility.Visible;

                isInNoChromeMode = false;

                if (InlineFrame.Content is IXboxInputPage)
                {
                    var page = ((IXboxInputPage)InlineFrame.Content);
                    //makes sure that if we go left from the inline frame, we end up selecting the current page's item in the nav list.
                    IEnumerable<NepAppUINavigationItem> items = (IEnumerable<NepAppUINavigationItem>)SplitViewNavigationList.ItemsSource;
                    var selectedItem = items.First(x => x.IsSelected);
                    var container = SplitViewNavigationList.ContainerFromItem(selectedItem);
                    page.SetLeftFocus((UIElement)container);

                    //set the upper focus to the hamburger menu
                    page.SetTopFocus(SplitViewOpenButton);
                }
            }
        }

        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
            }
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
            }
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
            InlineFrame.Focus(FocusState.Programmatic);

            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).RestoreFocus();
            }
        }

        private void HandleSplitViewPaneOpen()
        {
            if (InlineFrame.Content is IXboxInputPage)
            {
                ((IXboxInputPage)InlineFrame.Content).PreserveFocus();
            }

            SplitViewNavigationList.Focus(FocusState.Keyboard);

            var selectedNavItem = NepApp.UI.NavigationItems.FirstOrDefault(x => x.IsSelected);
            if (selectedNavItem != null)
            {
                //highlight and focus on the current nav item.
                var selectedNavContainer = SplitViewNavigationList.ContainerFromItem(selectedNavItem) as ContentPresenter;
                var selectedNavButton = VisualTreeHelper.GetChild(selectedNavContainer, 0) as RadioButton;
                selectedNavButton.Focus(FocusState.Keyboard);
            }
        }

        private void SplitViewOpenButton_Click(object sender, RoutedEventArgs e)
        {
            RootSplitView.IsPaneOpen = true;
            HandleSplitViewPaneOpen();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            //dismiss the menu if its open.
            if (RootSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
                RootSplitView.IsPaneOpen = false;
        }

        private void Page_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadY)
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                if (isInNoChromeMode) return;
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
            else if (e.Key == Windows.System.VirtualKey.GamepadView || e.Key == Windows.System.VirtualKey.GamepadMenu)
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible) return;
                //if (isInNoChromeMode) return;

                e.Handled = true;
                RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen;

                if (RootSplitView.IsPaneOpen)
                {
                    HandleSplitViewPaneOpen();
                }
                else
                {
                    HandleSplitViewPaneClose();
                }
            }
            //else if (e.Key == Windows.System.VirtualKey.Left)
            //{
            //    if (isInNoChromeMode) return;

            //    if (e.Handled) return;

            //    e.Handled = true;
            //    RootSplitView.IsPaneOpen = true;
            //    HandleSplitViewPaneOpen();
            //}
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
                    //todo implement handoff flyout.

                    App.Dispatcher.RunAsync(() =>
                    {
                        var handoffFlyout = Flyout.GetAttachedFlyout(HeaderControlHintIcon);
                        if (handoffFlyout != null)
                        {
                            handoffFlyout.ShowAt(HeaderControlHintIcon);
                            HandoffFlyoutSystemListBox.Focus(FocusState.Keyboard);
                        }
                    });

                    break;
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return new string[] { "ShowHandoffFlyout" };
        }

        private void HandoffListButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ListItemButton;

            if (btn.DataContext == null) return;

            this.GetViewModel<AppShellViewModel>()
                .HandoffFragment
                .HandOffCommand
                .Execute(btn.DataContext);
        }
    }
}
