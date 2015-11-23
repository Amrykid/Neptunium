using Crystal3.Model;
using Neptunium.Data.History;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using Neptunium.Data;
using Crystal3.Navigation;

namespace Neptunium.ViewModel
{
    public class NowPlayingViewViewModel : ViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            IsBusy = true;

            CurrentStation = ShoutcastStationMediaPlayer.CurrentStation;

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;

            if (ShoutcastStationMediaPlayer.SongMetadata != null)
                SongMetadata = ShoutcastStationMediaPlayer.SongMetadata.Track + " by " + ShoutcastStationMediaPlayer.SongMetadata.Artist;

            try
            {
                await LoadSongHistoryAsync();

                await LoadSongDataAsync();
            }
            catch (Exception) { }

            IsBusy = false;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            ShoutcastStationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
        }

        private void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            SongMetadata = e.Title + " by " + e.Artist;
        }

        private async Task LoadSongHistoryAsync()
        {
            if (ShoutcastStationMediaPlayer.CurrentStation != null)
            {
                var stream = ShoutcastStationMediaPlayer.CurrentStation.Streams.FirstOrDefault(x => x.HistoryPath != null);

                if (stream != null)
                {
                    var streamUrl = stream.Url;
                    var historyUrl = streamUrl.TrimEnd('/') + stream.HistoryPath;


                    switch (stream.ServerType)
                    {
                        case Data.StationModelStreamServerType.Shoutcast:
                            var historyItems = await Neptunium.Old_Hanasu.ShoutcastService.GetShoutcastStationSongHistoryAsync(ShoutcastStationMediaPlayer.CurrentStation, streamUrl);

                            HistoryItems = new ObservableCollection<HistoryItemModel>(historyItems.Select<Old_Hanasu.ShoutcastSongHistoryItem, HistoryItemModel>(x =>
                            {
                                var item = new HistoryItemModel();

                                item.Song = x.Song;
                                item.Time = x.LocalizedTime;

                                return item;
                            }));

                            break;

                    }
                }
            }
        }

        private async Task LoadSongDataAsync()
        {

        }


        public bool IsBusy { get { return GetPropertyValue<bool>(); } set { SetPropertyValue<bool>(value: value); } }

        public string SongMetadata
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue<string>(value: value); }
        }

        public ObservableCollection<HistoryItemModel> HistoryItems
        {
            get { return GetPropertyValue<ObservableCollection<HistoryItemModel>>(); }
            set { SetPropertyValue<ObservableCollection<HistoryItemModel>>(value: value); }
        }

        public StationModel CurrentStation { get { return GetPropertyValue<StationModel>(); } private set { SetPropertyValue<StationModel>(value: value); } }
    }
}
