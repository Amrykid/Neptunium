using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core;
using Crystal3.UI.Commands;
using Microsoft.HockeyApp;
using Neptunium.ViewModel.Dialog;
using Neptunium.ViewModel.Fragments;
using Neptunium.Core.Media.Metadata;
using Windows.System.UserProfile;
using Windows.Services.Store;
using Windows.Foundation;
using Windows.UI.Xaml;
using Neptunium.Core.Media.Audio;
using Neptunium.Core.Media;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel : ViewModelBase
    {
        public RelayCommand ResumePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.Resume();
        });

        public RelayCommand PausePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.Pause();
        });

        public SleepTimerContextFragment SleepTimerFragment => new SleepTimerContextFragment();
        public HandoffContextFragment HandoffFragment => new HandoffContextFragment();

        public RelayCommand MediaCastingCommand => new RelayCommand(async x =>
        {
            if (NepApp.MediaPlayer.IsMediaEngaged)
            {
                NepApp.MediaPlayer.ShowCastingPicker();
            }
            else
            {
                await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "You must be listening to sonething before you can cast it!");
            }
        });

        public RelayCommand GoToRemoteCommand => new RelayCommand(async x =>
        {
            var remotePage = NepApp.UI.NavigationItems.FirstOrDefault(X => X.NavigationViewModelType == typeof(ServerRemotePageViewModel));
            if (remotePage == null) throw new Exception("Remote page not found.");
            NepApp.UI.NavigateToItem(remotePage);
        });

        public AppShellViewModel()
        {

            NepApp.UI.AddNavigationRoute("Stations", typeof(StationsPageViewModel), ""); //"");
            NepApp.UI.AddNavigationRoute("Now Playing", typeof(NowPlayingPageViewModel), "");
            NepApp.UI.AddNavigationRoute("History", typeof(SongHistoryPageViewModel), "");
            NepApp.UI.AddNavigationRoute("Schedule", typeof(StationProgramsPageViewModel), "");
            NepApp.UI.AddNavigationRoute("Settings", typeof(SettingsPageViewModel), "");

#if !DEBUG
            if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowRemoteMenu))
            {
#endif
                NepApp.UI.AddNavigationRoute("Remote", typeof(ServerRemotePageViewModel), "");
#if !DEBUG
            }
#endif

            NepApp.UI.AddNavigationRoute("About", typeof(AboutPageViewModel), "");


            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;
            NepApp.MediaPlayer.FatalMediaErrorOccurred += MediaPlayer_FatalMediaErrorOccurred;
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;
            NepApp.MediaPlayer.Audio.HeadsetDetector.IsHeadsetPluggedInChanged += HeadsetDetector_IsHeadsetPluggedInChanged;

            if (NepApp.MediaPlayer.Audio.HeadsetDetector.IsHeadsetPluggedIn)
            {
                ShowOverlayForHeadphonesStatus(true);
            }

            if (UserProfilePersonalizationSettings.IsSupported())
            {
                NepApp.SongManager.ArtworkProcessor.SongArtworkAvailable += SongManager_SongArtworkAvailable;
                NepApp.SongManager.ArtworkProcessor.NoSongArtworkAvailable += SongManager_NoSongArtworkAvailable;
            }
        }

        private async void MediaPlayer_FatalMediaErrorOccurred(object sender, Windows.Media.Playback.MediaPlayerFailedEventArgs e)
        {
            await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");

            if (!await App.GetIfPrimaryWindowVisibleAsync())
            {
                NepApp.UI.Notifier.ShowErrorToastNotification(null, "Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");
            }
        }

        private void HeadsetDetector_IsHeadsetPluggedInChanged(object sender, EventArgs e)
        {
            bool isPluggedIn = ((IHeadsetDetector)sender).IsHeadsetPluggedIn;
            ShowOverlayForHeadphonesStatus(isPluggedIn);
        }

        private void ShowOverlayForHeadphonesStatus(bool isPluggedIn)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                NepApp.UI.Overlay.ShowSnackBarMessageAsync(isPluggedIn ? "Headphones connected." : "Headphones disconnected.");
            });
        }

        private async void SongManager_NoSongArtworkAvailable(object sender, Media.Songs.NepAppSongMetadataArtworkEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt))
            {
                //sets the fallback lockscreen image when we don't have any artwork available.
                if (e.ArtworkType == Media.Songs.NepAppSongMetadataBackground.Artist)
                {
                    try
                    {
                        await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private async void SongManager_SongArtworkAvailable(object sender, Media.Songs.NepAppSongMetadataArtworkEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt))
            {
                if (e.ArtworkType == Media.Songs.NepAppSongMetadataBackground.Artist)
                {
                    try
                    {
                        bool result = await NepApp.UI.LockScreen.TrySetLockScreenImageFromUriAsync(e.ArtworkUri);

                        if (!result)
                        {
                            await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                        }
                    }
                    catch (Exception)
                    {
                        //todo make and set an image that represents the lack of artwork. maybe a dark image with the app logo?
                        //maybe allow the user to set an image to use in this case.

                        await NepApp.UI.LockScreen.TrySetFallbackLockScreenImageAsync();
                    }
                }
            }
        }

        private async void SongManager_StationRadioProgramStarted(object sender, Media.Songs.NepAppStationProgramStartedEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
            {
                if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
                {
                    if (e.RadioProgram.Style == Core.Stations.StationProgramStyle.Block)
                    {
                        NepApp.UI.Notifier.ShowStationBlockProgrammingToastNotification(e.RadioProgram, e.Metadata);
                    }
                    else
                    {
                        NepApp.UI.Notifier.ShowStationHostedProgrammingToastNotification(e.RadioProgram, e.Metadata);
                    }
                }
                else
                {
                    if (e.RadioProgram.Style == Core.Stations.StationProgramStyle.Block)
                    {
                        await NepApp.UI.Overlay.ShowSnackBarMessageAsync("Tuning into " + e.RadioProgram.Name + " on " + e.Station);
                    }
                    else
                    {
                        await NepApp.UI.Overlay.ShowSnackBarMessageAsync("Tuning into " + e.RadioProgram.Name + " by " + e.RadioProgram.Host);
                    }
                }
            }

            if (e.Metadata != null)
                NepApp.UI.Notifier.UpdateLiveTile(new ExtendedSongMetadata(e.Metadata));
        }

        private async void SongManager_SongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
            {
                if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
                    NepApp.UI.Notifier.ShowSongToastNotification((ExtendedSongMetadata)e.Metadata);
            }

            NepApp.UI.Notifier.UpdateLiveTile((ExtendedSongMetadata)e.Metadata);
        }

        private void SongManager_PreSongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {

        }

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {

        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            base.OnNavigatedTo(sender, e);

            var stationsPage = NepApp.UI.NavigationItems.FirstOrDefault(X => X.NavigationViewModelType == typeof(StationsPageViewModel));
            if (stationsPage == null) throw new Exception("Stations page not found.");
            NepApp.UI.NavigateToItem(stationsPage);

            RaisePropertyChanged(nameof(ResumePlaybackCommand));


            //CheckForReverseHandoffOpportunitiesIfSupported();

#if RELEASE
            await CheckForUpdatesAsync();
#endif
        }

        private static async Task CheckForUpdatesAsync()
        {
            //https://docs.microsoft.com/en-us/windows/uwp/packaging/self-install-package-updates
            if (NepApp.Network.IsConnected && NepApp.Network.NetworkUtilizationBehavior == NepAppNetworkManager.NetworkDeterminedAppBehaviorStyle.Normal)
            {
                var storeContext = Windows.Services.Store.StoreContext.GetDefault();

                if (storeContext != null)
                {
                    var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                    if (updates.Count > 0)
                    {
                        if (await NepApp.UI.ShowYesNoDialogAsync("Updates available!", "There is an update to this application available. Would you like to install it?") == true)
                        {
                            var dialogController = await NepApp.UI.Overlay.ShowProgressDialogAsync("Update in progress", "Downloading updates....");

                            //download the updates

                            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation = storeContext.RequestDownloadStorePackageUpdatesAsync(updates);
                            downloadOperation.Progress = async (asyncInfo, progress) =>
                            {
                                await App.Dispatcher.RunWhenIdleAsync(() =>
                                {
                                    dialogController.SetDeterminateProgress(progress.PackageDownloadProgress);
                                });
                            };

                            var downloadResults = await downloadOperation.AsTask();

                            await dialogController.CloseAsync();

                            if (downloadResults.OverallState == StorePackageUpdateState.Completed)
                            {
                                //continue to installing updates
                                dialogController = await NepApp.UI.Overlay.ShowProgressDialogAsync("Update in progress", "Installing updates....");

                                dialogController.SetIndeterminate();
                                IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> installOperation = storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

                                StorePackageUpdateResult result = await installOperation.AsTask();

                                switch (result.OverallState)
                                {
                                    case StorePackageUpdateState.Completed:
                                        await NepApp.UI.ShowInfoDialogAsync("Update successfull", "Please restart this application. This application will now close.");
                                        Application.Current.Exit();
                                        break;
                                    default:
                                        // Get the failed updates.
                                        var failedUpdates = result.StorePackageUpdateStatuses.Where(
                                            status => status.PackageUpdateState != StorePackageUpdateState.Completed);

                                        await NepApp.UI.Overlay.ShowSnackBarMessageAsync("Updates failed to install.");
                                        break;
                                }
                            }
                            else
                            {
                                await NepApp.UI.Overlay.ShowSnackBarMessageAsync("Updates failed to download.");
                            }

                        }
                    }
                }
            }
        }

        //private async void CheckForReverseHandoffOpportunitiesIfSupported()
        //{
        //    if (NepApp.Handoff.IsInitialized)
        //    {
        //        var streamingDevices = await NepApp.Handoff.DetectStreamingDevicesAsync();

        //        if (streamingDevices == null) return; //nothing to see here.

        //        if (streamingDevices.Count > 0)
        //        {
        //            if (streamingDevices.Count == 1)
        //            {
        //                var streamingDevice = streamingDevices.First();
        //                await IoC.Current.Resolve<ISnackBarService>().ShowActionableSnackAsync(
        //                    "You're streaming on \'" + streamingDevice.Item1.DisplayName + "\'.",
        //                    "Transfer playback",
        //                    async x =>
        //                    {
        //                        //do reverse handoff
        //                        await DoReverseHandoff(streamingDevice);
        //                    },
        //                    10000);
        //            }
        //            else
        //            {
        //                await IoC.Current.ResolveDefault<ISnackBarService>()?.ShowSnackAsync("You have multiple devices streaming.", 3000);
        //            }
        //        }
        //    }
        //}
    }
}
