using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Audio
{
    public class NepAppAudioManager
    {
        private NepAppMediaPlayerManager mediaPlayer;
        public IHeadsetDetector HeadsetDetector { get; private set; }
        public NepAppAudioManager(NepAppMediaPlayerManager mediaPlayerManager)
        {
            mediaPlayer = mediaPlayerManager;

            HeadsetDetector = CreateHeadsetDetectorByPlatform();

            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
        }

        private async void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            if ((bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsWhenHeadphonesAreConnected) && HeadsetDetector.IsHeadsetPluggedIn)
            {
                await VoiceUtility.AnnonceSongMetadataUsingVoiceAsync(e.Metadata, VoiceMode.Headphones);
            }
        }

        private IHeadsetDetector CreateHeadsetDetectorByPlatform()
        {
            switch (Crystal3.CrystalApplication.GetDevicePlatform())
            {
                case Crystal3.Core.Platform.Mobile:
                    return new MobileHeadsetDetector();
                //case Crystal3.Core.Platform.Xbox:
                //    return new XboxHeadsetDetector();
                default:
                    return new MockHeadsetDetector();
            }
        }
    }
}
