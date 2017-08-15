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

        public AppShellViewModel()
        {
            App.Current.UnhandledException += Current_UnhandledException;

            NepApp.UI.AddNavigationRoute("Stations", typeof(StationsPageViewModel), "");
            NepApp.UI.AddNavigationRoute("Now Playing", typeof(NowPlayingPageViewModel), "");
        }

        private async void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception is NeptuniumException)
            {
                e.Handled = true;

                await NepApp.UI.ShowErrorDialogAsync("Uh-oh! Something went wrong!", e.Exception.Message);
            }
            else
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    HockeyClient.Current.TrackException(e.Exception);
                    HockeyClient.Current.Flush();
                }
            }
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
