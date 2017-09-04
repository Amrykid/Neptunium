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

namespace Neptunium.ViewModel
{
    public class AppShellViewModel: ViewModelBase
    {
        public RelayCommand ResumePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.Media.Resume();
        });

        public RelayCommand PausePlaybackCommand => new RelayCommand(x =>
        {
            NepApp.Media.Pause();
        });

        public RelayCommand ShowSleepTimerDialogCommand => new RelayCommand(x =>
        {
            NepApp.UI.Overlay.ShowDialogFragmentAsync<SleepTimerDialogFragment>();
        });

        public AppShellViewModel()
        {
            NepApp.UI.AddNavigationRoute("Stations", typeof(StationsPageViewModel), ""); //"");
            NepApp.UI.AddNavigationRoute("Now Playing", typeof(NowPlayingPageViewModel), "");
            NepApp.UI.AddNavigationRoute("History", typeof(SongHistoryPageViewModel), "");
            NepApp.UI.AddNavigationRoute("Settings", typeof(SettingsPageViewModel), "");

            NepApp.Media.IsPlayingChanged += Media_IsPlayingChanged;
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
        }
    }
}
