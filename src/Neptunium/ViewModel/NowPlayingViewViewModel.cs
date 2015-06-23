﻿using Crystal3.Model;
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

namespace Neptunium.ViewModel
{
    public class NowPlayingViewViewModel : ViewModelBase
    {
        protected override async void OnNavigatedTo(object sender, NavigationEventArgs e)
        {
            IsBusy = true;

            CurrentStation = ShoutcastStationMediaPlayer.CurrentStation != null ? ShoutcastStationMediaPlayer.CurrentStation.Name : "Not Playing Anything";

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

        private async Task LoadSongDataAsync()
        {

        }


        public bool IsBusy { get { return GetPropertyValue<bool>(); } set { SetPropertyValue<bool>(value: value); } }

        public ObservableCollection<HistoryItemModel> HistoryItems
        {
            get { return GetPropertyValue<ObservableCollection<HistoryItemModel>>(); }
            set { SetPropertyValue<ObservableCollection<HistoryItemModel>>(value: value); }
        }

        public string CurrentStation { get { return GetPropertyValue<string>(); } private set { SetPropertyValue<string>(value: value); } }
    }
}
