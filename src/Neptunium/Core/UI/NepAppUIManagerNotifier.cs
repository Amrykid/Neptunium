using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Neptunium.Core.Media.Metadata;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerNotifier
    {
        private ToastNotifier toastNotifier = null;
        private TileUpdater tileUpdater = null;
        internal NepAppUIManagerNotifier()
        {
            toastNotifier = ToastNotificationManager.CreateToastNotifier();
            tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
        }

        public void ShowSongToastNotification(SongMetadata metaData)
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
                                Text = metaData.StationPlayedOn?.Name,
                                HintStyle = AdaptiveTextStyle.Caption
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = metaData.StationPlayedOn?.StationLogoUrl.ToString(),
                        }
                    }
                }
            };

            var notification = new ToastNotification(content.GetXml());
            notification.Tag = "song-notif";
            notification.NotificationMirroring = NotificationMirroring.Disabled;
            toastNotifier.Show(notification);
        }
    }
}