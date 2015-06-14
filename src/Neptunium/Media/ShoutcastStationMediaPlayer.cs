using Neptunium.Data;
using Neptunium.MediaSourceStream;
using Neptunium.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace Neptunium.Media
{
    public static class ShoutcastStationMediaPlayer
    {
        static ShoutcastStationMediaPlayer()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        private static void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (var message in e.Data)
            {
                switch(message.Key)
                {
                    case Messages.MetadataChangedMessage:
                        {
                            var mcMessage = JsonHelper.FromJson<MetadataChangedMessage>(message.Value.ToString());

                            try
                            {
                                if (MetadataChanged != null)
                                    MetadataChanged(sender, new ShoutcastMediaSourceStreamMetadataChangedEventArgs(mcMessage.Track, mcMessage.Artist));
                            }
                            catch (Exception) { }

                            break;
                        }
                }
            }
        }

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
            if (IsPlaying)
            {
                var pause = new ValueSet();
                pause.Add(Messages.PauseMessage, "");

                BackgroundMediaPlayer.SendMessageToBackground(pause);
            }

            currentStationModel = station;

            var stream = station.Streams.First();

            var payload = new ValueSet();
            payload.Add(Messages.PlayStationMessage, JsonHelper.ToJson<PlayStationMessage>(new PlayStationMessage(stream.Url, stream.SampleRate, stream.RelativePath)));

            BackgroundMediaPlayer.SendMessageToBackground(payload);

        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
    }
}
