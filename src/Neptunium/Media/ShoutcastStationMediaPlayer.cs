using Neptunium.Data;
using Neptunium.MediaSourceStream;
using Neptunium.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace Neptunium.Media
{
    public static class ShoutcastStationMediaPlayer
    {
        static ShoutcastStationMediaPlayer()
        {
            if (!IsInitialized)
                Initialize();
        }

        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;

            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;

            IsInitialized = false;
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
                    case Messages.StationInfoMessage:
                        {
                            var siMessage = JsonHelper.FromJson<StationInfoMessage>(message.Value.ToString());

                            currentStationModel = StationDataManager.Stations.FirstOrDefault(x => x.Name == siMessage.CurrentStation);

                            if (CurrentStationChanged != null) CurrentStationChanged(null, EventArgs.Empty);

                            break;
                        }
                    case Messages.BackgroundAudioErrorMessage:
                        {
                            var baeMessage = JsonHelper.FromJson<BackgroundAudioErrorMessage>(message.Value.ToString());

                            //if (BackgroundAudioError != null)
                            //    BackgroundAudioError(null, EventArgs.Empty);

                            break;
                        }
                }
            }
        }

        private static StationModel currentStationModel = null;
        //private static ShoutcastMediaSourceStream currentStationMSSWrapper = null;

        public static StationModel CurrentStation { get { return currentStationModel; } }

        public static bool IsPlaying
        {
            get
            {
                var state = BackgroundMediaPlayer.Current.CurrentState;

                return state == MediaPlayerState.Opening || state == MediaPlayerState.Playing || state == MediaPlayerState.Buffering;
            }
        }

        //public static ShoutcastStationInfo StationInfoFromStream { get { return currentStationMSSWrapper?.StationInfo; } }

        public static async Task<bool> PlayStationAsync(StationModel station)
        {
            if (station == currentStationModel) return true;

            if (IsPlaying)
            {
                var pause = new ValueSet();
                pause.Add(Messages.PauseMessage, "");

                BackgroundMediaPlayer.SendMessageToBackground(pause);
            }

            currentStationModel = station;

            //TODO use a combo of events+anon-delegates and TaskCompletionSource to detect play back errors here to seperate connection errors from long-running audio errors.
            //handle error when connecting.
            TaskCompletionSource<object> errorTaskSource = new TaskCompletionSource<object>();
            TypedEventHandler<MediaPlayer, MediaPlayerFailedEventArgs> errorHandler = null;
            errorHandler = new TypedEventHandler<MediaPlayer, MediaPlayerFailedEventArgs>((MediaPlayer sender, MediaPlayerFailedEventArgs args) =>
            {
                //TODO extend said above magic to handle messages from the background audio player
                BackgroundMediaPlayer.Current.MediaFailed -= errorHandler;
                errorTaskSource.TrySetResult(false);
            });
            BackgroundMediaPlayer.Current.MediaFailed += errorHandler;

            //handle successful connection
            TaskCompletionSource<object> successTaskSource = new TaskCompletionSource<object>();
            TypedEventHandler<MediaPlayer, object> successHandler = null;
            successHandler = new TypedEventHandler<MediaPlayer, object>((MediaPlayer sender, object args) =>
            {
                if (sender.CurrentState == MediaPlayerState.Playing)
                {
                    BackgroundMediaPlayer.Current.CurrentStateChanged -= successHandler;
                    successTaskSource.TrySetResult(true);
                }
            });
            BackgroundMediaPlayer.Current.CurrentStateChanged += successHandler;


            var stream = station.Streams.First();

            var payload = new ValueSet();
            payload.Add(Messages.PlayStationMessage, JsonHelper.ToJson<PlayStationMessage>(new PlayStationMessage(stream.Url, stream.SampleRate, stream.RelativePath, station.Name)));

            BackgroundMediaPlayer.SendMessageToBackground(payload);

            if (successTaskSource.Task == await Task.WhenAny(errorTaskSource.Task, successTaskSource.Task))
            {
                //successful connection
                BackgroundMediaPlayer.Current.MediaFailed -= errorHandler;

                if (CurrentStationChanged != null) CurrentStationChanged(null, EventArgs.Empty);

                return true;
            }
            else
            {
                //unsuccessful connection
                BackgroundMediaPlayer.Current.CurrentStateChanged -= successHandler;

                if (BackgroundAudioError != null) BackgroundAudioError(null, EventArgs.Empty);

                return false;
            }
        }

        public static event EventHandler<ShoutcastMediaSourceStreamMetadataChangedEventArgs> MetadataChanged;
        public static event EventHandler CurrentStationChanged;
        public static event EventHandler BackgroundAudioError;
    }
}
