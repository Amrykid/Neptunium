using Crystal3.Model;
using Crystal3.UI.Commands;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel: ViewModelBase
    {
        public AppShellViewModel()
        {
            GoToStationsViewCommand = new CRelayCommand(x =>
            {
                Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                .NavigateTo<StationsViewViewModel>();
            });

            GoToNowPlayingViewCommand = new CRelayCommand(x =>
            {
                Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                .NavigateTo<NowPlayingViewViewModel>();
            });

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
        }

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            await App.Dispatcher.RunAsync(() =>
            {
                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                CurrentStation = ShoutcastStationMediaPlayer.CurrentStation.Name;
                CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
            });
        }

        public string CurrentSong { get { return GetPropertyValue<string>("CurrentSong"); } private set { SetPropertyValue<string>("CurrentSong", value); } }
        public string CurrentArtist { get { return GetPropertyValue<string>("CurrentArtist"); } private set { SetPropertyValue<string>("CurrentArtist", value); } }
        public string CurrentStation { get { return GetPropertyValue<string>("CurrentStation"); } private set { SetPropertyValue<string>("CurrentStation", value); } }
        public string CurrentStationLogo { get { return GetPropertyValue<string>("CurrentStationLogo"); } private set { SetPropertyValue<string>("CurrentStationLogo", value); } }

        public CRelayCommand GoToStationsViewCommand { get; private set; }
        public CRelayCommand GoToNowPlayingViewCommand { get; private set; }
    }
}
