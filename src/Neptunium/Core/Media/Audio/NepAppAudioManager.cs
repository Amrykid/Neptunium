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

        private void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {

        }

        private IHeadsetDetector CreateHeadsetDetectorByPlatform()
        {
            switch (Crystal3.DeviceInformation.GetDevicePlatform())
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
