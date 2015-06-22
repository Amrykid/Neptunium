using Crystal3.Model;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace Neptunium.ViewModel
{
    public class NowPlayingViewViewModel : ViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            IsBusy = true;

            try
            {
                await LoadSongHistoryAsync();

                await LoadSongDataAsync();
            }
            catch (Exception) { }

            IsBusy = false;
        }

        private async Task LoadSongHistoryAsync()
        {

            var stream = ShoutcastStationMediaPlayer.CurrentStation.Streams.First(x => x.HistoryPath != null);
            var streamUrl = stream.Url;
            var historyUrl = streamUrl.TrimEnd('/') + stream.HistoryPath;


            switch (stream.ServerType)
            {
                case Data.StationModelStreamServerType.Shoutcast:
                    //var historyItems = await Neptunium.Old_Hanasu.ShoutcastService.GetShoutcastStationSongHistoryAsync(ShoutcastStationMediaPlayer.CurrentStation, streamUrl);
                    break;

            }
        }

        private async Task LoadSongDataAsync()
        {

        }


        public bool IsBusy { get { return GetPropertyValue<bool>(); } set { SetPropertyValue<bool>(value: value); } }
    }
}
