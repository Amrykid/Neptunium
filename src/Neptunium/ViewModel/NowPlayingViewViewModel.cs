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
using Microsoft.Toolkit.Uwp.Notifications;
using Crystal3;
using Windows.Storage;
using Crystal3.Core;
using Windows.Data.Xml.Dom;
using Hqub.MusicBrainz.API.Entities;
using Hqub.MusicBrainz.API;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Crystal3.UI.Commands;
using Windows.System;
using System.Diagnostics;
using Neptunium.Managers;
using Microsoft.HockeyApp.DataContracts;
using Neptunium.Managers.Songs;

namespace Neptunium.ViewModel
{
    public class NowPlayingViewViewModel : UIViewModelBase
    {
        public NowPlayingViewViewModel()
        {
            ViewAlbumOnMusicBrainzCommand = new ManualRelayCommand(async x =>
            {
                //await Launcher.LaunchUriAsync(new Uri("https://musicbrainz.org/release/" + CurrentSongAlbumData.AlbumID));
            });

            PlayPauseCommand = new RelayCommand(item =>
            {
                if (StationMediaPlayer.IsPlaying)
                    StationMediaPlayer.Pause();
                else
                    StationMediaPlayer.Play();
            });

            NextStationCommand = new RelayCommand(item => { });
            PreviousStationCommand = new RelayCommand(item => { });
        }

        private async void ShoutcastStationMediaPlayer_BackgroundAudioError(object sender, EventArgs e)
        {
            await Crystal3.CrystalApplication.Dispatcher.RunAsync(() =>
            {
                CurrentArtist = "";
                CurrentSong = "";
                CurrentStation = null;
                CurrentAlbum = null;
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
                    NowPlayingBackgroundImage = StationMediaPlayer.CurrentStation.Logo.ToString();
                }

                HistoryItems?.Clear();

                IsBusy = false;
            });
        }

        protected override async void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            CurrentStation = StationMediaPlayer.CurrentStation;

            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (Neptunium.Managers.Songs.SongManager.CurrentSong != null)
            {
                var song = Neptunium.Managers.Songs.SongManager.CurrentSong;

                CurrentSong = song.Track;
                CurrentArtist = song.Artist;

                SongMetadata = song.Track + " by " + song.Artist;

                if (App.GetDevicePlatform() != Platform.Xbox)
                    if (!App.IsUnrestrictiveInternetConnection()) return;

                UpdateCoverImage(song);
            }

            SongManager.PreSongChanged += SongManager_PreSongChanged;
            SongManager.SongChanged += SongManager_SongChanged;
        }

        private void UpdateCoverImage(SongMetadata song)
        {
            var albumUrl = song.MBData?.Album?.AlbumCoverUrl;
            if (string.IsNullOrWhiteSpace(albumUrl))
                albumUrl = song.ITunesData?.Album?.AlbumCoverUrl;

            if (!string.IsNullOrWhiteSpace(albumUrl))
                CoverImage = new Uri(albumUrl);
            else
                CoverImage = new Uri(CurrentStation.Logo);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            SongManager.SongChanged -= SongManager_SongChanged;
            SongManager.PreSongChanged -= SongManager_PreSongChanged;
            StationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError;
        }

        private async void SongManager_PreSongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            await App.Dispatcher.RunAsync(() =>
            {
                SongMetadata = e.Metadata.Track + " by " + e.Metadata.Artist;

                CurrentSong = e.Metadata.Track;
                CurrentArtist = e.Metadata.Artist;

                if (StationMediaPlayer.CurrentStation != null)
                {
                    CurrentStation = StationMediaPlayer.CurrentStation;
                    NowPlayingBackgroundImage = StationMediaPlayer.CurrentStation.Logo?.ToString();
                }

                UpdateCoverImage(e.Metadata);
            });
        }

        private async void SongManager_SongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            if (CurrentSong == e.Metadata.Track && CurrentArtist == e.Metadata.Artist)
            {
                await App.Dispatcher.RunAsync(() =>
                {
                    UpdateCoverImage(e.Metadata);
                });

            }
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
        public Uri CurrentAlbum { get { return GetPropertyValue<Uri>(); } private set { SetPropertyValue<Uri>(value: value); } }
        public Uri CoverImage { get { return GetPropertyValue<Uri>(); } private set { SetPropertyValue<Uri>(value: value); } }

        public string NowPlayingBackgroundImage { get { return GetPropertyValue<string>(); } set { SetPropertyValue<string>(value: value); } }

        public RelayCommand PlayPauseCommand { get; private set; }
        public RelayCommand PreviousStationCommand { get; private set; }
        public RelayCommand NextStationCommand { get; private set; }
    }
}
