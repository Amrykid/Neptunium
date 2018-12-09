using Crystal3;
using Neptunium.Core.Media.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Neptunium.Core.Media.Bluetooth
{
    public class NepAppMediaBluetoothManager
    {
        public NepAppMediaBluetoothDeviceCoordinator DeviceCoordinator { get; private set; }
        public bool IsBluetoothModeActive { get; private set; }

        private SongMetadata lastAnnouncedSongMetadata = null;

        public NepAppMediaBluetoothManager(Neptunium.Media.NepAppMediaPlayerManager playerManager)
        {
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox) return; //bluetooth control is not supported on xbox.

            DeviceCoordinator = new NepAppMediaBluetoothDeviceCoordinator();

            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;

            DeviceCoordinator.IsBluetoothConnectedChanged += DeviceCoordinator_IsBluetoothConnectedChanged;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DeviceCoordinator.InitializeAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async void SongManager_PreSongChanged(object sender, Neptunium.Media.Songs.NepAppSongChangedEventArgs e)
        {
            if (e.Metadata == null) return;
            if (e.Metadata.IsUnknownMetadata) return;

            if (lastAnnouncedSongMetadata != null)
            {
                if (lastAnnouncedSongMetadata.Equals(e.Metadata)) return;
            }

            if (IsBluetoothModeActive)
            {
                if ((bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsInBluetoothMode))
                {
                    lastAnnouncedSongMetadata = e.Metadata;
                    await VoiceUtility.AnnonceSongMetadataUsingVoiceAsync(e, VoiceMode.Bluetooth);
                }
            }
        }

        private void DeviceCoordinator_IsBluetoothConnectedChanged(object sender, NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs e)
        {
            IsBluetoothModeActive = e.IsConnected;

            //Sets the media volume to be lower so that the song notifications voice sounds louder by comparison.
            NepApp.MediaPlayer.SetVolume(NepApp.MediaPlayer.IsMediaEngaged ? 0.5 : 1.0);
        }
    }
}
