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
using Neptunium.ViewModel;

namespace Neptunium.Fragments
{
    public class NowPlayingViewFragment : ViewModelFragment
    {
        public NowPlayingViewFragment()
        {
            CurrentStation = ShoutcastStationMediaPlayer.CurrentStation;

            ShoutcastStationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            ShoutcastStationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (ShoutcastStationMediaPlayer.SongMetadata != null)
                SongMetadata = ShoutcastStationMediaPlayer.SongMetadata.Track + " by " + ShoutcastStationMediaPlayer.SongMetadata.Artist;
        }

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, EventArgs e)
        {
            await Crystal3.CrystalApplication.Dispatcher.RunAsync(() =>
            {
                CurrentArtist = "";
                CurrentSong = "";
                CurrentStation = null;
                CurrentStationLogo = null;
            });

        }

        private void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                SongMetadata = e.Title + " by " + e.Artist;


                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }

                AppShellViewModel.UpdateLiveTile();
            });
        }


        private async void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsBusy = true;

                if (ShoutcastStationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = ShoutcastStationMediaPlayer.CurrentStation;
                    CurrentStationLogo = ShoutcastStationMediaPlayer.CurrentStation.Logo.ToString();
                }

                HistoryItems?.Clear();
            });

            try
            {
                await LoadSongHistoryAsync();

                await LoadSongDataAsync();
            }
            catch (Exception) { }

            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsBusy = false;
            });
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
                            //var historyItems = await Neptunium.Old_Hanasu.ShoutcastService.GetShoutcastStationSongHistoryAsync(ShoutcastStationMediaPlayer.CurrentStation, streamUrl);

                            //HistoryItems = new ObservableCollection<HistoryItemModel>(historyItems.Select<Old_Hanasu.ShoutcastSongHistoryItem, HistoryItemModel>(x =>
                            //{
                            //    var item = new HistoryItemModel();

                            //    item.Song = x.Song;
                            //    item.Time = x.LocalizedTime;

                            //    return item;
                            //}));

                            break;

                    }
                }
            }
        }

        private async Task LoadSongDataAsync()
        {

        }

        public override void Invoke(ViewModelBase viewModel, object data)
        {

        }

        public override void Dispose()
        {
            ShoutcastStationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
            ShoutcastStationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;
            ShoutcastStationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError;
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

        public string CurrentSong { get { return GetPropertyValue<string>("CurrentSong"); } private set { SetPropertyValue<string>("CurrentSong", value); } }
        public string CurrentArtist { get { return GetPropertyValue<string>("CurrentArtist"); } private set { SetPropertyValue<string>("CurrentArtist", value); } }
        public string CurrentStationLogo { get { return GetPropertyValue<string>("CurrentStationLogo"); } private set { SetPropertyValue<string>("CurrentStationLogo", value); } }
    }
}
