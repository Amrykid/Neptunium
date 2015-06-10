using Neptunium.Data;
using Neptunium.MediaSourceStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Neptunium.Media
{
    public static class ShoutcastStationMediaPlayer
    {
        private static StationModel currentStationModel = null;
        private static ShoutcastMediaSourceStream currentStationMSSWrapper = null;

        public static StationModel CurrentStation { get { return currentStationModel; } }

        public static bool IsPlaying
        {
            get
            {
                var state = BackgroundMediaPlayer.Current.CurrentState;

                return state == MediaPlayerState.Opening || state == MediaPlayerState.Playing || state == MediaPlayerState.Buffering;
            }
        }

        public static ShoutcastStationInfo StationInfoFromStream { get { return currentStationMSSWrapper?.StationInfo; } }

        public static async Task PlayStationAsync(StationModel station)
        {
            if (IsPlaying && currentStationMSSWrapper != null)
            {
                BackgroundMediaPlayer.Current.Pause();
                BackgroundMediaPlayer.Shutdown();
                currentStationMSSWrapper.Disconnect();

                currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;
            }

            currentStationModel = station;

            var stream = station.Streams.First();

            currentStationMSSWrapper = new ShoutcastMediaSourceStream(new Uri(stream.Url));

            currentStationMSSWrapper.MetadataChanged += CurrentStationMSSWrapper_MetadataChanged;

            await currentStationMSSWrapper.ConnectAsync();

            BackgroundMediaPlayer.Current.SetMediaSource(currentStationMSSWrapper.MediaStreamSource);

            await Task.Delay(500);

            BackgroundMediaPlayer.Current.Play();

            var mediaControls = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            mediaControls.IsChannelDownEnabled = false;
            mediaControls.IsChannelUpEnabled = false;
            mediaControls.IsFastForwardEnabled = false;
            mediaControls.IsNextEnabled = false;
            mediaControls.IsPauseEnabled = true;
            mediaControls.IsPlayEnabled = true;
            mediaControls.IsPreviousEnabled = false;
            mediaControls.IsRecordEnabled = false;
            mediaControls.IsRewindEnabled = false;
            mediaControls.IsStopEnabled = false;
        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;

        private static void CurrentStationMSSWrapper_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            try
            {
                if (MetadataChanged != null)
                    MetadataChanged(sender, e);
            }
            catch (Exception) { }

            var mediaDisplay = BackgroundMediaPlayer.Current.SystemMediaTransportControls.DisplayUpdater;

            mediaDisplay.Type = Windows.Media.MediaPlaybackType.Music;
            mediaDisplay.MusicProperties.Title = e.Title;
            mediaDisplay.MusicProperties.Artist = e.Artist;

            mediaDisplay.Update();
        }
    }
}
