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

namespace Neptunium.Fragments
{
    public class NowPlayingViewFragment : ViewModelFragment
    {
        public NowPlayingViewFragment()
        {
            CurrentStation = StationMediaPlayer.CurrentStation;

            StationMediaPlayer.MetadataChanged += ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged += ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError += ShoutcastStationMediaPlayer_BackgroundAudioError;

            if (StationMediaPlayer.SongMetadata != null)
                SongMetadata = StationMediaPlayer.SongMetadata.Track + " by " + StationMediaPlayer.SongMetadata.Artist;

            ViewAlbumOnMusicBrainzCommand = new ManualRelayCommand(async x =>
            {
                await Launcher.LaunchUriAsync(new Uri("https://musicbrainz.org/release/" + CurrentSongAlbumData.AlbumID));
            });
        }

        internal void ShowSongNotification()
        {
            try
            {
                if (StationMediaPlayer.IsPlaying && StationMediaPlayer.SongMetadata != null)
                {
                    var nowPlaying = StationMediaPlayer.SongMetadata;

                    var toastHistory = ToastNotificationManager.History.GetHistory();

                    if (toastHistory.Count > 0)
                    {
                        var latestToast = toastHistory.FirstOrDefault();

                        if (latestToast != null)
                        {
                            var track = latestToast.Content.LastChild.FirstChild.FirstChild.FirstChild.LastChild.InnerText as string;

                            if (track == nowPlaying.Track) return;
                        }
                    }

                    ToastContent content = new ToastContent()
                    {
                        Launch = "nowPlaying",
                        Audio = new ToastAudio()
                        {
                            Silent = true,
                        },
                        Duration = CrystalApplication.GetDevicePlatform() == Platform.Mobile ? ToastDuration.Short : ToastDuration.Long,
                        Visual = new ToastVisual()
                        {
                            BindingGeneric = new ToastBindingGeneric()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = nowPlaying.Track,
                                        HintStyle = AdaptiveTextStyle.Title
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = nowPlaying.Artist,
                                        HintStyle = AdaptiveTextStyle.Body
                                    }
                                },
                                HeroImage = new ToastGenericHeroImage()
                                {
                                    Source = CurrentSongAlbumData != null ? CurrentSongAlbumData.AlbumCoverUrl : StationMediaPlayer.CurrentStation?.Logo,
                                    AlternateText = StationMediaPlayer.CurrentStation?.Name,
                                },
                            }
                        }
                    };

                    XmlDocument doc = content.GetXml();
                    ToastNotification notification = new ToastNotification(doc);
                    notification.NotificationMirroring = NotificationMirroring.Disabled;
                    notification.Tag = "nowPlaying";
                    notification.ExpirationTime = DateTime.Now.AddMinutes(5); //songs usually aren't this long.

                    ToastNotificationManager.CreateToastNotifier().Show(notification);
                }
            }
            catch (Exception)
            {

            }
        }

        public void UpdateLiveTile()
        {
            var tiler = TileUpdateManager.CreateTileUpdaterForApplication();

            TileBindingContentAdaptive bindingContent = null;

            if (StationMediaPlayer.IsPlaying && StationMediaPlayer.SongMetadata != null)
            {
                var nowPlaying = StationMediaPlayer.SongMetadata;

                bindingContent = new TileBindingContentAdaptive()
                {
                    PeekImage = new TilePeekImage()
                    {
                        Source = CurrentSongAlbumData != null ? CurrentSongAlbumData.AlbumCoverUrl : StationMediaPlayer.CurrentStation?.Logo,
                        AlternateText = StationMediaPlayer.CurrentStation?.Name,
                        HintCrop = TilePeekImageCrop.None
                    },
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = nowPlaying.Track,
                            HintStyle = AdaptiveTextStyle.Body
                        },

                        new AdaptiveText()
                        {
                            Text = nowPlaying.Artist,
                            HintWrap = true,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        }
                    }
                };
            }
            else
            {
                bindingContent = new TileBindingContentAdaptive()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = "Ready to stream",
                            HintStyle = AdaptiveTextStyle.Body
                        },

                        new AdaptiveText()
                        {
                            Text = "Tap to get started.",
                            HintWrap = true,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        }
                    }
                };
            }

            TileBinding binding = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,

                DisplayName = StationMediaPlayer.IsPlaying ? "Now Playing" : "Neptunium",

                Content = bindingContent
            };

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = binding,
                    TileWide = binding,
                    TileLarge = binding
                }
            };

            var tile = new TileNotification(content.GetXml());
            tiler.Update(tile);

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

                UpdateLiveTile();
            });

            var timeOut = Task.Delay(2000);

            var albumDataTask = TryFindArtistOnMusicBrainzAsync(e.Title, e.Artist);

            Task resultTask = null;

            if ((resultTask = await Task.WhenAny(albumDataTask, timeOut)) != timeOut)
            {
                //update immediately since we didn't timeout
                await UpdateAlbumDataFromTaskAsync(albumDataTask);
            }

            if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] == true)
            {
                await App.Dispatcher.RunWhenIdleAsync(() =>
                {
                    if (!Windows.UI.Xaml.Window.Current.Visible)
                        ShowSongNotification();
                });
            }

            if (resultTask == timeOut)
            {
                //second chance to update data after the fact.
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

        private async Task<AlbumData> TryFindArtistOnMusicBrainzAsync(string track, string artist)
        {
            AlbumData data = new AlbumData();
            try
            {
                var recordingQuery = new Hqub.MusicBrainz.API.QueryParameters<Hqub.MusicBrainz.API.Entities.Recording>();
                recordingQuery.Add("artistname", artist);
                recordingQuery.Add("country", "JP");
                recordingQuery.Add("recording", track);

                var recordings = await Recording.SearchAsync(recordingQuery);

                foreach (var potentialRecording in recordings?.Items)
                {
                    if (potentialRecording.Title.ToLower().StartsWith(track.ToLower()))
                    {
                        var firstRelease = potentialRecording.Releases.Items.FirstOrDefault();

                        if (firstRelease != null)
                        {
                            try
                            {
                                data.AlbumCoverUrl = CoverArtArchive.GetCoverArtUri(firstRelease.Id)?.ToString();
                            }
                            catch (Exception) { }

                            data.Artist = potentialRecording.Credits.First().Artist.Name;
                            data.ArtistID = potentialRecording.Credits.First().Artist.Id;
                            data.Album = firstRelease.Title;
                            data.AlbumID = firstRelease.Id;

                            return data;
                        }
                    }
                }

            }
            catch (Exception)
            {
                return null;
            }

            return null;
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
            });

            UpdateLiveTile();

            await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                IsBusy = false;
            });
        }

        public override void Invoke(ViewModelBase viewModel, object data)
        {

        }

        public override void Dispose()
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
