using Crystal3;
using Crystal3.Core;
using Crystal3.InversionOfControl;
using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Crystal3.UI.MessageDialog;
using Neptunium.Data;
using Neptunium.Fragments;
using Neptunium.Managers;
using Neptunium.Media;
using Neptunium.Services.SnackBar;
using Neptunium.Shared;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Navigation;
using Windows.Services.Store;
using Windows.Networking.Connectivity;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel : UIViewModelBase
    {
        private NavigationService InlineNavigationService = null;
        public AppShellViewModel()
        {
            if (!IoC.Current.IsRegistered<IMessageDialogService>())
                IoC.Current.Register<IMessageDialogService>(new DefaultMessageDialogService());

            GoToStationsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<StationsViewViewModel>())
                    InlineNavigationService.NavigateTo<StationsViewViewModel>();
            });

            GoToNowPlayingViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<NowPlayingViewViewModel>())
                    InlineNavigationService.NavigateTo<NowPlayingViewViewModel>();
            });

            GoToSettingsViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<SettingsViewViewModel>())
                    InlineNavigationService.NavigateTo<SettingsViewViewModel>();
            });

            GoToSongHistoryViewCommand = new RelayCommand(x =>
            {
                if (!InlineNavigationService.IsNavigatedTo<SongHistoryViewModel>())
                    InlineNavigationService.NavigateTo<SongHistoryViewModel>();
            });

            PlayCommand = new RelayCommand(x =>
            {
                BackgroundMediaPlayer.Current.Play();
            }, x =>
            {
                var currentPlayerState = BackgroundMediaPlayer.Current.PlaybackSession.PlaybackState;

                return currentPlayerState != MediaPlaybackState.Buffering &&
                currentPlayerState != MediaPlaybackState.Opening &&
                currentPlayerState != MediaPlaybackState.Playing;
            });

            PauseCommand = new RelayCommand(x =>
            {
                if (BackgroundMediaPlayer.Current.PlaybackSession.CanPause)
                    BackgroundMediaPlayer.Current.Pause();
            }, x => { try { return BackgroundMediaPlayer.Current.PlaybackSession.CanPause; } catch (Exception) { return true; } });


            HandoffStationCommand = new ManualRelayCommand(x =>
            {

            });

            HandOffViewFragment = new HandOffFlyoutViewFragment();
            NowPlayingView = new NowPlayingViewFragment();
            SleepTimerViewFragment = new SleepTimerFlyoutViewFragment();

            WindowManager.GetStatusManagerForCurrentWindow().NormalStatusText = "Neptunium"; //"Hanasu Alpha";

            UpdateLiveTile();
        }

        private async void CarModeManager_CarModeManagerCarModeStatusChanged(object sender, CarModeManagerCarModeStatusChangedEventArgs e)
        {
            await UpdateCarModeButtonStatusAsync(e.IsInCarMode);
        }

        private async Task UpdateCarModeButtonStatusAsync(bool IsInCarMode)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                if (ShowCarModeStatusButton != IsInCarMode)
                {
                    IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync((IsInCarMode ? "Car Mode Activated" : "Car Mode Deactivated"), 3000);

                    ShowCarModeStatusButton = IsInCarMode;
                }
            });
        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            InlineNavigationService = Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two);

            if (!StationMediaPlayer.IsInitialized)
                await StationMediaPlayer.InitializeAsync();

            StationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;
            StationMediaPlayer.BackgroundAudioReconnecting += StationMediaPlayer_BackgroundAudioReconnecting;

            if (CrystalApplication.GetDevicePlatform() == Platform.Mobile)
            {
                CarModeManager.CarModeManagerCarModeStatusChanged += CarModeManager_CarModeManagerCarModeStatusChanged;

                if (!CarModeManager.IsInitialized)
                    await CarModeManager.InitializeAsync();

                UpdateCarModeButtonStatusAsync(CarModeManager.IsInCarMode).Forget();
            }

            StationMediaPlayer.ConnectingStatusChanged += StationMediaPlayer_ConnectingStatusChanged;

            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

            ContinuedAppExperienceManager.RemoteSystemsListUpdated += ContinuedAppExperienceManager_RemoteSystemsListUpdated;

            while (App.MessageQueue.Count > 0)
                await IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync(App.MessageQueue.Dequeue());

#if !DEBUG
            var storeContext = StoreContext.GetDefault();
            if (storeContext != null)
            {
                var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                
                if (updates.Count > 0)
                {
                    await IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync("There is an update available for this application.");
                }
            }
#endif
        }

        private async void StationMediaPlayer_BackgroundAudioReconnecting(object sender, EventArgs e)
        {
            string title = "Heads up!";
            string message = "Network change detected! Reconnecting!";

            bool appVisible = await App.GetIfPrimaryWindowVisibleAsync();

            if (appVisible)
            {
                await await App.Dispatcher.RunWhenIdleAsync(() => IoC.Current.Resolve<ISnackBarService>().ShowSnackAsync(message));
            }
            else
            {
                ShowMediaInfoNotification(title, message);
            }
        }

        private void ContinuedAppExperienceManager_RemoteSystemsListUpdated(object sender, EventArgs e)
        {
            ContinuedAppExperienceManager.RemoteSystemsListUpdated -= ContinuedAppExperienceManager_RemoteSystemsListUpdated;
            ContinuedAppExperienceManager.CheckForReverseHandoffOpportunities();
            ContinuedAppExperienceManager.RemoteSystemsListUpdated += ContinuedAppExperienceManager_RemoteSystemsListUpdated;
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            HandoffStationCommand.SetCanExecute(sender.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            StationMediaPlayer.ConnectingStatusChanged -= StationMediaPlayer_ConnectingStatusChanged;

            base.OnNavigatedFrom(sender, e);
        }


        private void StationMediaPlayer_ConnectingStatusChanged(object sender, StationMediaPlayerConnectingStatusChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() => IsBusy = e.IsConnecting);
        }

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, ShoutcastStationMediaPlayerBackgroundAudioErrorEventArgs e)
        {
            StationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError; //throttle error messages

            string title = "What the goodness?!";
            string message = "An error occured while trying stream this station.";

            if (e.ClosedReason == Windows.Media.Core.MediaStreamSourceClosedReason.AppReportedError)
            {
                if (!App.IsInternetConnected())
                {
                    //assume its because we lost our internet connection.
                    message = "We lost our connection to the internet!";
                }
                else
                {
                    if (e.Exception != null)
                    {
                        message += "\r\n" + e.Exception.Message;
                    }
                }
            }

            bool appVisible = await App.GetIfPrimaryWindowVisibleAsync();

            if (appVisible)
            {
                await IoC.Current.Resolve<IMessageDialogService>().ShowAsync(message, title);
            }
            else
            {
                ShowMediaErrorNotification(title, message);
            }

            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;
        }


        private void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            UpdateLiveTile();
        }

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Contains(e.Title)) return; //don't play that pre-defined station message that happens every so often.

            if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] == true)
            {
                await App.Dispatcher.RunWhenIdleAsync(() =>
                {
                    if (!App.GetIfPrimaryWindowVisible())
                        ShowSongNotification();
                });
            }

            UpdateLiveTile();
        }

        internal void ShowMediaErrorNotification(string title, string message)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = title,
                                    HintStyle = AdaptiveTextStyle.Title,
                                },
                                new AdaptiveText()
                                {
                                    Text = message,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            },
                    }
                }
            };

            XmlDocument doc = content.GetXml();
            ToastNotification notification = new ToastNotification(doc);
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            notification.Tag = "mediaError";

            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        internal void ShowMediaInfoNotification(string title, string message)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = title,
                                    HintStyle = AdaptiveTextStyle.Title,
                                },
                                new AdaptiveText()
                                {
                                    Text = message,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            },
                    }
                }
            };

            XmlDocument doc = content.GetXml();
            ToastNotification notification = new ToastNotification(doc);
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            notification.Tag = "mediaInfo";

            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        internal void ShowSongNotification()
        {

            if (StationMediaPlayer.IsPlaying && StationMediaPlayer.SongMetadata != null)
            {
                var nowPlaying = StationMediaPlayer.SongMetadata;

                var toastHistory = ToastNotificationManager.History.GetHistory();

                if (toastHistory.Count > 0)
                {
                    var latestToast = toastHistory.FirstOrDefault();

                    if (latestToast != null)
                    {
                        var track = latestToast.Content.LastChild.FirstChild.FirstChild.FirstChild.LastChild.InnerText as string;

                        if (track == nowPlaying.Track) return;
                    }
                }

                ToastContent content = new ToastContent()
                {
                    Launch = "nowPlaying",
                    Audio = new ToastAudio()
                    {
                        Silent = true,
                    },
                    Duration = CrystalApplication.GetDevicePlatform() == Platform.Mobile ? ToastDuration.Short : ToastDuration.Long,
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = nowPlaying.Track,
                                        HintStyle = AdaptiveTextStyle.Title
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = nowPlaying.Artist,
                                        HintStyle = AdaptiveTextStyle.Body
                                    }
                                },
                            HeroImage = new ToastGenericHeroImage()
                            {
                                Source = StationMediaPlayer.CurrentStation?.Logo,
                                AlternateText = StationMediaPlayer.CurrentStation?.Name,
                            },
                        }
                    }
                };

                XmlDocument doc = content.GetXml();
                ToastNotification notification = new ToastNotification(doc);
                notification.NotificationMirroring = NotificationMirroring.Disabled;
                notification.Tag = "nowPlaying";
                notification.ExpirationTime = DateTime.Now.AddMinutes(5); //songs usually aren't this long.

                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }

        public static void UpdateLiveTile()
        {
            var tiler = TileUpdateManager.CreateTileUpdaterForApplication();

            TileBindingContentAdaptive bindingContent = null;

            if (StationMediaPlayer.IsPlaying && StationMediaPlayer.SongMetadata != null)
            {
                var nowPlaying = StationMediaPlayer.SongMetadata;

                bindingContent = new TileBindingContentAdaptive()
                {
                    PeekImage = new TilePeekImage()
                    {
                        Source = StationMediaPlayer.CurrentStation?.Logo,
                        AlternateText = StationMediaPlayer.CurrentStation?.Name,
                        HintCrop = TilePeekImageCrop.None
                    },
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = nowPlaying.Track,
                            HintStyle = AdaptiveTextStyle.Body
                        },

                        new AdaptiveText()
                        {
                            Text = nowPlaying.Artist,
                            HintWrap = true,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        }
                    }
                };
            }
            else
            {
                bindingContent = new TileBindingContentAdaptive()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = "Ready to stream",
                            HintStyle = AdaptiveTextStyle.Body
                        },

                        new AdaptiveText()
                        {
                            Text = "Tap to get started.",
                            HintWrap = true,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        }
                    }
                };
            }

            TileBinding binding = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,

                DisplayName = StationMediaPlayer.IsPlaying ? "Now Playing" : "Neptunium",

                Content = bindingContent
            };

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = binding,
                    TileWide = binding,
                    TileLarge = binding
                }
            };

            var tile = new TileNotification(content.GetXml());
            tiler.Update(tile);

        }


        public RelayCommand GoToStationsViewCommand { get; private set; }
        public RelayCommand GoToNowPlayingViewCommand { get; private set; }
        public RelayCommand GoToSettingsViewCommand { get; private set; }
        public RelayCommand GoToSongHistoryViewCommand { get; private set; }

        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }

        public ManualRelayCommand HandoffStationCommand { get; private set; }

        public HandOffFlyoutViewFragment HandOffViewFragment { get; private set; }
        public NowPlayingViewFragment NowPlayingView { get; private set; }
        public SleepTimerFlyoutViewFragment SleepTimerViewFragment { get; private set; }

        public bool ShowCarModeStatusButton
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue<bool>(value: value); }
        }
    }
}
