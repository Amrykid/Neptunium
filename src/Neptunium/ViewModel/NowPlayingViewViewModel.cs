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
using Windows.UI.Notifications;
using NotificationsExtensions.Toasts;
using Crystal3;
using NotificationsExtensions;
using Windows.Storage;
using Crystal3.Core;
using Windows.Data.Xml.Dom;
using NotificationsExtensions.Tiles;
using Hqub.MusicBrainz.API.Entities;
using Hqub.MusicBrainz.API;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Crystal3.UI.Commands;
using Windows.System;
using System.Diagnostics;
using Neptunium.Managers;

namespace Neptunium.ViewModel
{
    public class NowPlayingViewViewModel : UIViewModelBase
    {
        public NowPlayingViewViewModel()
        {
            ViewAlbumOnMusicBrainzCommand = new ManualRelayCommand(async x =>
            {
                await Launcher.LaunchUriAsync(new Uri("https://musicbrainz.org/release/" + CurrentSongAlbumData.AlbumID));
            });
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

        private async void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (StationMediaPlayer.CurrentStation.StationMessages.Contains(e.Title)) return; //ignore that pre-defined station message that happens every so often.

            if (!string.IsNullOrWhiteSpace(e.Title) && string.IsNullOrWhiteSpace(e.Artist))
            {
                //station message got through.

#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#else
                return;
#endif
            }

            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                SongMetadata = e.Title + " by " + e.Artist;


                CurrentSong = e.Title;
                CurrentArtist = e.Artist;

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    CurrentStationLogo = StationMediaPlayer.CurrentStation.Logo.ToString();
                }
            });

            var timeOut = Task.Delay(4000);

            var albumDataTask = SongMetadataManager.FindAlbumDataAsync(e.Title, e.Artist);

            Task resultTask = null;

            if ((resultTask = await Task.WhenAny(albumDataTask, timeOut)) != timeOut)
            {
                //update immediately since we didn't timeout
                await UpdateAlbumDataFromTaskAsync(albumDataTask);
            }
        }

        private async Task UpdateAlbumDataFromTaskAsync(Task<AlbumData> albumDataTask)
        {
            await App.Dispatcher.RunAsync(async () =>
            {
                try
                {
                    CurrentSongAlbumData = await albumDataTask;

                    var displayUp = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater;

                    if (CurrentSongAlbumData != null)
                    {
                        displayUp.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CurrentSongAlbumData.AlbumCoverUrl));
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(CurrentStationLogo))
                            displayUp.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CurrentStationLogo));
                        else
                            displayUp.Thumbnail = null;
                    }

                    ViewAlbumOnMusicBrainzCommand.SetCanExecute(CurrentSongAlbumData != null);

                    displayUp.Update();
                }
                catch (Exception) { }
            });
        }

        private async void ShoutcastStationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsBusy = true;

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    CurrentStationLogo = StationMediaPlayer.CurrentStation.Logo.ToString();
                }

                HistoryItems?.Clear();

                IsBusy = false;
            });
        }

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            CurrentStation = StationMediaPlayer.CurrentStation;

            StationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (StationMediaPlayer.SongMetadata != null)
                SongMetadata = StationMediaPlayer.SongMetadata.Track + " by " + StationMediaPlayer.SongMetadata.Artist;
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            StationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError;
        }

        public ManualRelayCommand ViewAlbumOnMusicBrainzCommand { get; private set; }

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

        public AlbumData CurrentSongAlbumData { get { return GetPropertyValue<AlbumData>(); } set { SetPropertyValue<AlbumData>(value: value); } }
    }
}
