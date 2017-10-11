using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Neptunium.Core.Media.Metadata;
using System;
using Neptunium.Core.Stations;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerNotifier
    {
        private ToastNotifier toastNotifier = null;
        private TileUpdater tileUpdater = null;
        public const string SongNotificationTag = "song-notif";

        internal NepAppUIManagerNotifier()
        {
            toastNotifier = ToastNotificationManager.CreateToastNotifier();
            tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
        }

        public void ShowGenericToastNotification(string title, string message, string tag)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = message,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = tag;
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }

        public void ShowSongToastNotification(ExtendedSongMetadata metaData)
        {
            ToastContent content = new ToastContent()
            {
                Launch = "now-playing",
                Audio = new ToastAudio()
                {
                    Silent = true,
                },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = metaData.Track,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = metaData.Artist,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },

                            new AdaptiveText()
                            {
                                Text = metaData.StationPlayedOn,
                                HintStyle = AdaptiveTextStyle.Caption
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = !string.IsNullOrWhiteSpace(metaData.Album?.AlbumCoverUrl) ? metaData.Album?.AlbumCoverUrl : metaData.StationLogo.ToString(),
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = SongNotificationTag;
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }

        internal void ShowErrorToastNotification(StationStream stream, string title, string message)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = message,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = stream.ParentStation?.StationLogoUrl.ToString(),
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = "error";
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }

        public void UpdateLiveTile(ExtendedSongMetadata nowPlaying)
        {
            var tiler = TileUpdateManager.CreateTileUpdaterForApplication();

            TileBindingContentAdaptive largeBindingContent = new TileBindingContentAdaptive()
            {
                PeekImage = new TilePeekImage()
                {
                    Source = nowPlaying.Album?.AlbumCoverUrl?.ToString() ?? nowPlaying.StationLogo.ToString(),
                    AlternateText = nowPlaying.StationPlayedOn,
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

            TileBindingContentAdaptive mediumBindingContent = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = nowPlaying.Album?.AlbumCoverUrl?.ToString() ?? nowPlaying.StationLogo.ToString(),
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

            TileBindingContentAdaptive smallBindingContent = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = nowPlaying.Album?.AlbumCoverUrl?.ToString() ?? nowPlaying.StationLogo.ToString(),
                }
            };

            Func<TileBindingContentAdaptive, TileBinding> createBinding = (TileBindingContentAdaptive con) =>
            {
                return new TileBinding()
                {
                    Branding = TileBranding.NameAndLogo,

                    DisplayName = "Now Playing on Neptunium",

                    Content = con,

                    ContentId = nowPlaying.Track.GetHashCode().ToString()
                };
            };

            

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = createBinding(smallBindingContent),
                    TileWide = createBinding(mediumBindingContent),
                    TileLarge = createBinding(largeBindingContent)
                }
            };

            var tile = new TileNotification(content.GetXml());
            tiler.Update(tile);
        }

        internal void ShowStationProgrammingToastNotification(StationProgram program, SongMetadata metadata)
        {
            ToastContent content = new ToastContent()
            {
                Launch = "now-playing",
                Audio = new ToastAudio()
                {
                    Silent = true,
                },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "Turning into " + metadata.Track,
                                HintStyle = AdaptiveTextStyle.Title
                            },

                            new AdaptiveText()
                            {
                                Text = "Hosted by " + program.Host,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },

                            new AdaptiveText()
                            {
                                Text = metadata.StationPlayedOn,
                                HintStyle = AdaptiveTextStyle.Caption
                            },
                        },
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = SongNotificationTag;
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }
    }
}