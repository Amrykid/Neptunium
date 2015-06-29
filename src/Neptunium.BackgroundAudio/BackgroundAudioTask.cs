using Neptunium.MediaSourceStream;
using Neptunium.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;

namespace Neptunium.BackgroundAudio
{
    //https://github.com/Microsoft/Windows-universal-samples/blob/master/backgroundaudio/cs/backgroundaudiotask/mybackgroundaudiotask.cs

    public sealed class BackgroundAudioTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls smtc;
        private ShoutcastMediaSourceStream currentStationMSSWrapper = null;
        private string currentStationServerType = null;
        private string currentStation = null;
        private IBackgroundTaskInstance thisTaskInstance = null;
        private string currentTrack = "Title";
        private string currentArtist = "Artist";
        private volatile bool appIsInForeground = false;

        public BackgroundAudioTask()
        {

        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            thisTaskInstance = taskInstance;

            smtc = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            smtc.ButtonPressed += Smtc_ButtonPressed;
            smtc.PropertyChanged += Smtc_PropertyChanged;


            smtc.IsChannelDownEnabled = false;
            smtc.IsChannelUpEnabled = false;
            smtc.IsFastForwardEnabled = false;
            smtc.IsNextEnabled = false;
            smtc.IsPauseEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsPreviousEnabled = false;
            smtc.IsRecordEnabled = false;
            smtc.IsRewindEnabled = false;
            smtc.IsStopEnabled = false;


            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            //BackgroundMediaPlayer.Current.MediaFailed += Current_MediaFailed;
            taskInstance.Canceled += TaskInstance_Canceled;
            taskInstance.Task.Completed += Task_Completed;
        }

        private void DetachAllHandlers()
        {
            smtc.ButtonPressed -= Smtc_ButtonPressed;
            smtc.PropertyChanged -= Smtc_PropertyChanged;

            try
            {
                BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayer_MessageReceivedFromForeground;
            }
            catch (Exception) { }
            BackgroundMediaPlayer.Current.CurrentStateChanged -= Current_CurrentStateChanged;
            //BackgroundMediaPlayer.Current.MediaFailed -= Current_MediaFailed;
            thisTaskInstance.Canceled -= TaskInstance_Canceled;
            thisTaskInstance.Task.Completed -= Task_Completed;
        }

        //private void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        //{
        //    try
        //    {
        //        var payload = new ValueSet();
        //        payload.Add(Messages.BackgroundAudioErrorMessage, JsonHelper.ToJson<BackgroundAudioErrorMessage>(new BackgroundAudioErrorMessage(args)));

        //        BackgroundMediaPlayer.SendMessageToForeground(payload);
        //    }
        //    catch (Exception)
        //    { }
        //    finally
        //    {
        //        FullStop();

        //        currentStationMSSWrapper = null;
        //    }

        //}

        private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            switch (args.Property)
            {
                case SystemMediaTransportControlsProperty.SoundLevel:
                    break;
            }
        }

        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Play();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");

                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");

                    break;
            }

        }

        private void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            DetachAllHandlers();

            deferral.Complete();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            FullStop();

            DetachAllHandlers();

            deferral.Complete();
        }

        private void FullStop()
        {
            if (currentStationMSSWrapper != null && (currentStationServerType == "Shoutcast" || currentStationServerType == "Icecast"))
                currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

            BackgroundMediaPlayer.Shutdown();

            if (currentStationMSSWrapper != null && (currentStationServerType == "Shoutcast" || currentStationServerType == "Icecast"))
                currentStationMSSWrapper.Disconnect();
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
            else if (sender.CurrentState == MediaPlayerState.Closed)
            {
                smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
            }

        }

        private async void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            try
            {
                foreach (var message in e.Data)
                {
                    switch (message.Key)
                    {
                        case Messages.PlayStationMessage:
                            {
                                ShoutcastMediaSourceStream lastStream = null;
                                if (currentStationMSSWrapper != null && (currentStationServerType == "Shoutcast" || currentStationServerType == "Icecast"))
                                {
                                    currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                                    lastStream = currentStationMSSWrapper;

                                    BackgroundMediaPlayer.Current.Pause();
                                }

                                var psMessage = JsonHelper.FromJson<PlayStationMessage>(message.Value.ToString());

                                var streamUrl = psMessage.StreamUrl;

                                var sampleRate = psMessage.SampleRate;
                                var relativePath = psMessage.RelativePath;

                                currentStation = psMessage.StationName;

                                currentStationServerType = psMessage.ServerType;

                                if (currentStationServerType == "Direct")
                                {
                                    BackgroundMediaPlayer.Current.SetUriSource(new Uri(streamUrl));

                                    currentTrack = "Unknown Song";
                                    currentArtist = "Unknown Artist";

                                    UpdateNowPlaying(currentTrack, currentArtist);
                                }
                                else if ((currentStationServerType == "Shoutcast" || currentStationServerType == "Icecast"))
                                {
                                    currentStationMSSWrapper = new ShoutcastMediaSourceStream(new Uri(streamUrl));

                                    currentStationMSSWrapper.MetadataChanged += CurrentStationMSSWrapper_MetadataChanged;


                                    await currentStationMSSWrapper.ConnectAsync(uint.Parse(sampleRate.ToString()), relativePath.ToString());

                                    BackgroundMediaPlayer.Current.SetMediaSource(currentStationMSSWrapper.MediaStreamSource);

                                    await Task.Delay(500);

                                    BackgroundMediaPlayer.Current.Play();

                                    if (lastStream != null) lastStream.Disconnect();

                                }
                            }
                            break;

                        case Messages.AppLaunchOrResume:
                            {
                                appIsInForeground = true;

                                var payload = new ValueSet();
                                payload.Add(Messages.StationInfoMessage, JsonHelper.ToJson<StationInfoMessage>(new StationInfoMessage(currentStation)));


                                payload.Add(Messages.MetadataChangedMessage, JsonHelper.ToJson<MetadataChangedMessage>(
                                    new MetadataChangedMessage(currentTrack, currentArtist)));


                                BackgroundMediaPlayer.SendMessageToForeground(payload);

                                break;
                            }
                        case Messages.AppSuspend:
                            {
                                appIsInForeground = false;

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                if (currentStationMSSWrapper != null && (currentStationServerType == "Shoutcast" || currentStationServerType == "Icecast"))
                {
                    currentStationMSSWrapper.MetadataChanged -= CurrentStationMSSWrapper_MetadataChanged;

                    currentStationMSSWrapper = null;
                }

                var payload = new ValueSet();
                payload.Add(Messages.BackgroundAudioErrorMessage, JsonHelper.ToJson<BackgroundAudioErrorMessage>(new BackgroundAudioErrorMessage(ex)));

                BackgroundMediaPlayer.SendMessageToForeground(payload);
            }
        }



        private void CurrentStationMSSWrapper_MetadataChanged(object sender, ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            lock (currentTrack)
            {
                lock (currentArtist)
                {
                    currentTrack = e.Title;
                    currentArtist = e.Artist;
                }
            }

            UpdateNowPlaying(currentTrack, currentArtist);

        }

        private void UpdateNowPlaying(string track, string artist)
        {
            try
            {
                smtc.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                smtc.DisplayUpdater.MusicProperties.Title = track;
                smtc.DisplayUpdater.MusicProperties.Artist = artist;

                smtc.DisplayUpdater.AppMediaId = currentStation;

                smtc.DisplayUpdater.Update();
            }
            catch (Exception) { }

            var payload = new ValueSet();
            payload.Add(Messages.MetadataChangedMessage, JsonHelper.ToJson<MetadataChangedMessage>(new MetadataChangedMessage(track, artist)));

            BackgroundMediaPlayer.SendMessageToForeground(payload);
        }
    }
}
