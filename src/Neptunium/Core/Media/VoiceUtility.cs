using Crystal3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

namespace Neptunium.Core.Media
{
    public static class VoiceUtility
    {
        private static SemaphoreSlim announcementLock = new SemaphoreSlim(1);

        private static SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private static VoiceInformation japaneseFemaleVoice = null;
        private static VoiceInformation koreanFemaleVoice = null;

        public static event EventHandler SongAnnouncementFinished;

        public static async Task AnnonceSongMetadataUsingVoiceAsync(Neptunium.Media.Songs.NepAppSongChangedEventArgs e, VoiceMode voiceMode)
        {
            try
            {
                if (e.Metadata.IsUnknownMetadata) return;

                if (japaneseFemaleVoice == null)
                {
                    japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female && x.DisplayName.Contains("Haruka"));
                }

                if (koreanFemaleVoice == null)
                {
                    koreanFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                        x.Language.ToLower().StartsWith("kr") && x.Gender == VoiceGender.Female);
                }

                //todo make this a utility in its own class.
                await announcementLock.WaitAsync();

                var nowPlayingSsmlData = GenerateSongAnnouncementSsml(e.Metadata.Artist, e.Metadata.Track, NepApp.MediaPlayer.CurrentStream.ParentStation.PrimaryLocale);
                var stream = await speechSynth.SynthesizeSsmlToStreamAsync(nowPlayingSsmlData);

                if (voiceMode == VoiceMode.Bluetooth)
                {
                    double initialVolume = NepApp.MediaPlayer.Volume;
                    bool shouldFade = initialVolume >= 0.1;

                    if (shouldFade) await NepApp.MediaPlayer.FadeVolumeDownToAsync(0.1);
                    await PlayAnnouncementAudioStreamAsync(stream, voiceMode);
                    if (shouldFade) await NepApp.MediaPlayer.FadeVolumeUpToAsync(initialVolume);
                }
                else
                {
                    NepApp.MediaPlayer.Pause();
                    await PlayAnnouncementAudioStreamAsync(stream, voiceMode);
                    NepApp.MediaPlayer.Resume();
                }

                announcementLock.Release();

                SongAnnouncementFinished?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {

            }
        }

        private static async Task PlayAnnouncementAudioStreamAsync(SpeechSynthesisStream stream, VoiceMode voiceMode)
        {
            await await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
            {
                var source = MediaSource.CreateFromStream(stream, stream.ContentType);

                var media = new MediaPlayer();

                //media.AudioCategory = MediaPlayerAudioCategory.Speech;
                if (voiceMode == VoiceMode.Bluetooth)
                {
                    media.AudioCategory = MediaPlayerAudioCategory.Media; //Speech is too low.
                }
                else if (voiceMode == VoiceMode.Headphones)
                {
                    media.AudioCategory = MediaPlayerAudioCategory.Alerts;
                }

                media.CommandManager.IsEnabled = false;
                media.Volume = 1.0;

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

        private static string GenerateSongAnnouncementSsml(string artist, string title, string locale = "ja")
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
            switch (locale.ToLower().Trim())
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

    public enum VoiceMode
    {
        Bluetooth,
        Headphones
    }
}
