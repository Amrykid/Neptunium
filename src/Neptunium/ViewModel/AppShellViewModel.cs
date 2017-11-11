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

        public RelayCommand MediaCastingCommand => new RelayCommand(x =>
        {
            NepApp.MediaPlayer.ShowCastingPicker();
        });

        public AppShellViewModel()
        {
            NepApp.UI.AddNavigationRoute("Stations", typeof(StationsPageViewModel), ""); //"");
            NepApp.UI.AddNavigationRoute("Now Playing", typeof(NowPlayingPageViewModel), "");
            NepApp.UI.AddNavigationRoute("History", typeof(SongHistoryPageViewModel), "");
            NepApp.UI.AddNavigationRoute("Settings", typeof(SettingsPageViewModel), "");

            NepApp.MediaPlayer.IsPlayingChanged += Media_IsPlayingChanged;
            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
            NepApp.SongManager.SongChanged += SongManager_SongChanged;
            NepApp.SongManager.StationRadioProgramStarted += SongManager_StationRadioProgramStarted;

            if (UserProfilePersonalizationSettings.IsSupported())
                NepApp.SongManager.SongArtworkAvailable += SongManager_SongArtworkAvailable;
        }

        private async void SongManager_SongArtworkAvailable(object sender, Media.Songs.NepAppSongMetadataArtworkEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.UpdateLockScreenWithSongArt))
            {
                if (e.ArtworkType == Media.Songs.NepAppSongMetadataBackground.Artist)
                {
                    try
                    {
                        bool result = await NepApp.UI.LockScreen.TrySetLockScreenImageFromUri(e.ArtworkUri);

                        if (!result)
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private async void SongManager_StationRadioProgramStarted(object sender, Media.Songs.NepAppStationProgramStartedEventArgs e)
        {
            if (!await App.GetIfPrimaryWindowVisibleAsync()) //if the primary window isn't visible
            {
                if ((bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications))
                    NepApp.UI.Notifier.ShowStationProgrammingToastNotification(e.RadioProgram, e.Metadata);
            }

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

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            base.OnNavigatedTo(sender, e);

            var stationsPage = NepApp.UI.NavigationItems.FirstOrDefault(X => X.NavigationViewModelType == typeof(StationsPageViewModel));
            if (stationsPage == null) throw new Exception("Stations page not found.");
            NepApp.UI.NavigateToItem(stationsPage);

            RaisePropertyChanged(nameof(ResumePlaybackCommand));

            //CheckForReverseHandoffOpportunitiesIfSupported();
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
