using Crystal3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Neptunium.Core.Media.Bluetooth
{
    public class NepAppMediaBluetoothManager
    {
        private SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private VoiceInformation japaneseFemaleVoice = null;
        private VoiceInformation koreanFemaleVoice = null;
        public NepAppMediaBluetoothDeviceCoordinator DeviceCoordinator { get; private set; }
        public bool IsBluetoothModeActive { get; private set; }

        public NepAppMediaBluetoothManager(Neptunium.Media.NepAppMediaPlayerManager playerManager)
        {
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox) return; //bluetooth control is not supported on xbox.

            DeviceCoordinator = new NepAppMediaBluetoothDeviceCoordinator();

            japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female && x.DisplayName.Contains("Haruka"));

            koreanFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                x.Language.ToLower().StartsWith("kr") && x.Gender == VoiceGender.Female);

            playerManager.CurrentMetadataChanged += Media_CurrentMetadataChanged;

            DeviceCoordinator.IsBluetoothConnectedChanged += DeviceCoordinator_IsBluetoothConnectedChanged;
            DeviceCoordinator.InitializeAsync();
        }

        private void DeviceCoordinator_IsBluetoothConnectedChanged(object sender, NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs e)
        {
            IsBluetoothModeActive = e.IsConnected;
        }

        private async void Media_CurrentMetadataChanged(object sender, Neptunium.Media.NepAppMediaPlayerManagerCurrentMetadataChangedEventArgs e)
        {
            if (IsBluetoothModeActive)
            {
                if ((bool)NepApp.Settings.GetSetting(AppSettings.SaySongNotificationsInBluetoothMode))
                {

                    var nowPlayingSsmlData = GenerateSongAnnouncementSsml(e.Metadata.Artist, e.Metadata.Track, NepApp.Media.CurrentStream.ParentStation.PrimaryLocale);
                    var stream = await speechSynth.SynthesizeSsmlToStreamAsync(nowPlayingSsmlData);

                    double initialVolume = NepApp.Media.Volume;
                    bool shouldFade = initialVolume >= 0.1;

                    if (shouldFade) await NepApp.Media.FadeVolumeDownToAsync(0.1);
                    await PlayAnnouncementAudioStreamAsync(stream);             
                    if (shouldFade) await NepApp.Media.FadeVolumeUpToAsync(initialVolume);
                }
            }
        }

        private async Task PlayAnnouncementAudioStreamAsync(SpeechSynthesisStream stream)
        {
            await await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
            {
                var source = MediaSource.CreateFromStream(stream, stream.ContentType);

                var media = new MediaPlayer();

                media.CommandManager.IsEnabled = false;
                media.Volume = 1.0;
                //media.AudioCategory = MediaPlayerAudioCategory.Speech;
                media.AudioCategory = MediaPlayerAudioCategory.Media; //Speech is too low.
                media.Source = source;

                Task mediaOpenTask = media.WaitForMediaOpenAsync();

                media.Play();

                await mediaOpenTask;

                await Task.Delay((int)media.PlaybackSession.NaturalDuration.TotalMilliseconds);

                source.Dispose();

                stream.Dispose();

                media.Dispose();
            });
        }

        private string GenerateSongAnnouncementSsml(string artist, string title, string locale = "ja")
        {
            StringBuilder builder = new StringBuilder();

            var englishVoice = SpeechSynthesizer.AllVoices.Where(x => x.Language.ToLower().StartsWith("en")).First(x => x.Gender == VoiceGender.Female);

            var phrases = new string[] {
                "Now Playing: {1} by {0}",
                "And Now: {1} by {0}",
                "Playing: {1} by {0}",
            };
            //                "君が{0}の{1}を聞いています"

            var randomizer = new Random(DateTime.Now.Millisecond);
            var index = randomizer.Next(0, phrases.Length - 1);

            var phrase = phrases[index];

            bool nativeVoiceAvailable = false;
            switch(locale.ToLower().Trim())
            {
                case "ja":
                    nativeVoiceAvailable = japaneseFemaleVoice != null;
                    break;
                case "kr":
                    nativeVoiceAvailable = koreanFemaleVoice != null;
                    break;
            }

            if (index == 3 && nativeVoiceAvailable)
                index = 1;

            builder.AppendLine(@"<?xml version='1.0' encoding='ISO-8859-1'?>");
            builder.AppendLine(@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>");
            builder.AppendLine("<voice gender='female'>");

            Action<string> speakInEnglish = (text) =>
            {
                builder.AppendLine(text);
            };
            Action<string> speakInJapanese = (text) =>
            {
                //var voice = (japaneseVoiceAvailable ? japaneseFemaleVoice : SpeechSynthesizer.DefaultVoice);

                //builder.AppendLine("<voice xml:lang='" + voice.Language + "' name='" + voice.DisplayName + "'>");
                builder.AppendLine("<s>");
                builder.AppendLine("<voice" + (japaneseFemaleVoice != null ? " name='" + japaneseFemaleVoice.DisplayName + "'" : " gender='female'") + ">");

                builder.AppendLine(text);
                builder.AppendLine("</voice>");
                builder.AppendLine("</s>");
            };
            Action<string> speakInKorean = (text) =>
            {
                builder.AppendLine("<s>");
                builder.AppendLine("<voice" + (koreanFemaleVoice != null ? " name='" + koreanFemaleVoice.DisplayName + "'" : " gender='female'") + ">");

                builder.AppendLine(text);
                builder.AppendLine("</voice>");
                builder.AppendLine("</s>");
            };

            Action<string> speakInNativeLanguage = (text) =>
            {
                if (nativeVoiceAvailable)
                {
                    switch (locale.ToLower().Trim())
                    {
                        case "ja":
                            speakInJapanese(text);
                            break;
                        case "kr":
                            speakInKorean(text);
                            break;
                    }
                }
                else
                {
                    //fallback to english
                    speakInEnglish(text);
                }
            };

            switch (index)
            {
                case 0:
                    speakInEnglish("Now Playing:");
                    speakInNativeLanguage(title);
                    speakInEnglish("By");
                    speakInNativeLanguage(artist);
                    break;
                case 1:
                    speakInEnglish("And Now:");
                    speakInNativeLanguage(title);
                    speakInEnglish("By");
                    speakInNativeLanguage(artist);
                    break;
                case 2:
                    speakInEnglish("Playing:");
                    speakInNativeLanguage(title);
                    speakInEnglish("By");
                    speakInNativeLanguage(artist);
                    break;
            }
            builder.AppendLine("</voice>");
            builder.AppendLine("</speak>");
            return builder.ToString();
        }
    }
}
