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
using Neptunium.Core.Media.Metadata;
using Windows.System.UserProfile;
using Windows.Services.Store;
using Windows.Foundation;
using Windows.UI.Xaml;
using Neptunium.Core.Media.Audio;
using Neptunium.Core.Media;
using Neptunium.Core.Stations;

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

        public RelayCommand SleepTimerCommand => new RelayCommand(async x =>
        {
            if (NepApp.MediaPlayer.IsMediaEngaged)
            {
                await NepApp.UI.Overlay.ShowDialogFragmentAsync<SleepTimerDialogFragment>();
            }
            else
            {
                await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "You must be listening to sonething before you can set the sleep timer!");
            }
        });

        public RelayCommand MediaCastingCommand => new RelayCommand(async x =>
        {
            if (NepApp.MediaPlayer.IsMediaEngaged)
            {
                await NepApp.UI.Overlay.ShowDialogFragmentAsync<StationHandoffDialogFragment>();
            }
            else
            {
                await NepApp.UI.ShowInfoDialogAsync("Can't do that!", "You must be listening to sonething before you can cast it or hand it off!");
            }
        });

        public RelayCommand GoToRemoteCommand => new RelayCommand(x =>
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
            NepApp.MediaPlayer.Audio.HeadsetDetector.IsHeadsetPluggedInChanged += HeadsetDetector_IsHeadsetPluggedInChanged;

            if (NepApp.MediaPlayer.Audio.HeadsetDetector.IsHeadsetPluggedIn)
            {
                ShowOverlayForHeadphonesStatus(true);
            }
        }

        private async void MediaPlayer_FatalMediaErrorOccurred(object sender, Windows.Media.Playback.MediaPlayerFailedEventArgs e)
        {
            await NepApp.UI.ShowInfoDialogAsync("Uh-Oh!", !NepApp.Network.IsConnected ? "Network connection lost!" : "An unknown error occurred.");
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

        private void Media_IsPlayingChanged(object sender, Media.NepAppMediaPlayerManager.NepAppMediaPlayerManagerIsPlayingEventArgs e)
        {

        }
#if RELEASE
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
#else
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
#endif
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

            AskToStreamLastPlayedStation();
        }

        private static void AskToStreamLastPlayedStation()
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Xbox)
            {
                if (!string.IsNullOrWhiteSpace(NepApp.Stations.LastPlayedStationName))
                {
                    NepApp.UI.Overlay.ShowSnackBarMessageWithCallbackAsync(
                        string.Format("Continue streaming {0}?", NepApp.Stations.LastPlayedStationName),
                        "Yes",
                        TimeSpan.FromSeconds(30),
                        async msg =>
                        {
                            await StreamLastPlayedStationAsync();
                        });
                }
            }
            else
            {
                //for xbox, we'll do something different.
            }
        }

        private static async Task StreamLastPlayedStationAsync()
        {
            var station = await NepApp.Stations.GetStationByNameAsync(NepApp.Stations.LastPlayedStationName);
            if (station != null)
            {
                var controller = await NepApp.UI.Overlay.ShowProgressDialogAsync(string.Format("Connecting to {0}...", station.Name), "Please wait...");
                controller.SetIndeterminate();

                try
                {
                    StationStream stream = null;
                    if (stream == null)
                    {
                        //check if we need to automatically choose a lower bitrate.
                        if ((int)NepApp.Network.NetworkUtilizationBehavior < 2) //check if we're on "conservative" or "opt-in"
                        {
                            //grab the stream with the lowest bitrate
                            stream = station.Streams.OrderBy(x => x.Bitrate).First();
                        }
                        else
                        {
                            stream = station.Streams.OrderByDescending(x => x.Bitrate).First(); //otherwise, grab a higher bitrate
                        }
                    }

                    await NepApp.MediaPlayer.TryStreamStationAsync(stream);
                }
                catch (Exception ex)
                {
                    await NepApp.UI.ShowInfoDialogAsync("Uh-oh! Couldn't do that!", ex.Message);
                }
                finally
                {
                    await controller.CloseAsync();
                }
            }
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
