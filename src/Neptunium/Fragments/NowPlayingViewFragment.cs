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
                                    Source = StationMediaPlayer.CurrentStation?.Logo,
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
                        Source = StationMediaPlayer.CurrentStation?.Logo,
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

        private void ShoutcastStationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
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


            if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.ShowSongNotifications] == true)
            {
                //var artistSearch = await Hqub.MusicBrainz.API.Entities.Artist.SearchAsync(e.Artist, 5, 0);
                //foreach(var potentialArtist in artistSearch.Items.Where(x => x.Country.ToLower().Contains("japan")))
                //{
                //    var x = potentialArtist;
                //}

                ShowSongNotification();
            }

            UpdateLiveTile();
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
            if (StationMediaPlayer.CurrentStation != null)
            {
                var stream = StationMediaPlayer.CurrentStation.Streams.FirstOrDefault(x => x.HistoryPath != null);

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
            StationMediaPlayer.MetadataChanged -= ShoutcastStationMediaPlayer_MetadataChanged;
            StationMediaPlayer.CurrentStationChanged -= ShoutcastStationMediaPlayer_CurrentStationChanged;
            StationMediaPlayer.BackgroundAudioError -= ShoutcastStationMediaPlayer_BackgroundAudioError;
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
