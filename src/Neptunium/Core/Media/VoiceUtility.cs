using Crystal3;
using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;

namespace Neptunium.Core.Media
{
    public static class VoiceUtility
    {
        private static SemaphoreSlim announcementLock = new SemaphoreSlim(1);

        private static SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private static VoiceInformation japaneseFemaleVoice = null;
        private static VoiceInformation koreanFemaleVoice = null;
        private static MediaElement announcementMediaElement = new MediaElement();

        public static event EventHandler SongAnnouncementFinished;

        public static Task AnnonceSongMetadataUsingVoiceAsync(Neptunium.Media.Songs.NepAppSongChangedEventArgs e, VoiceMode voiceMode)
        {
            return AnnonceSongMetadataUsingVoiceAsync(e.Metadata, voiceMode);
        }
        public static async Task AnnonceSongMetadataUsingVoiceAsync(SongMetadata songMetadata, VoiceMode voiceMode)
        {
            try
            {
                if (songMetadata.IsUnknownMetadata) return;

                if (japaneseFemaleVoice == null)
                {
                    japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language.ToLower().StartsWith("ja") 
                        && x.Gender == VoiceGender.Female && x.DisplayName.Contains("Haruka"));
                }

                if (koreanFemaleVoice == null)
                {
                    koreanFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                        x.Language.ToLower().StartsWith("kr") && x.Gender == VoiceGender.Female);
                }

                await announcementLock.WaitAsync();

                var currentStation = await NepApp.Stations.GetStationByNameAsync(NepApp.MediaPlayer.CurrentStream.ParentStation);


                string artistName = FindAppropriateArtistName(songMetadata, currentStation);
                var nowPlayingSsmlData = GenerateSongAnnouncementSsml(artistName, songMetadata.Track, currentStation?.PrimaryLocale ?? "JP");


                var stream = await speechSynth.SynthesizeSsmlToStreamAsync(nowPlayingSsmlData);

                double initialVolume = NepApp.MediaPlayer.Volume;
                bool shouldFade = initialVolume >= 0.1;

                if (shouldFade) await NepApp.MediaPlayer.FadeVolumeDownToAsync(0.1);
                await PlayAnnouncementAudioStreamAsync(stream, voiceMode);
                if (shouldFade) await NepApp.MediaPlayer.FadeVolumeUpToAsync(initialVolume);

                announcementLock.Release();

                SongAnnouncementFinished?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception)
            {

            }
        }

        private static string FindAppropriateArtistName(SongMetadata songMetadata, StationItem stationItem)
        {
            //This method tries to find a localized name for the artist, if applicable. This makes speech sound more natural.

            var builtInArtist = NepApp.MetadataManager.FindBuiltInArtist(songMetadata.Artist, stationItem.PrimaryLocale ?? "jp");

            if (builtInArtist == null) return songMetadata.Artist;

            //prefer native language over english name, if possible.

            if (builtInArtist.AltNames?.Length > 0)
            {
                foreach(var name in builtInArtist.AltNames.Where(x => x.NameLanguage.ToLower() != "en"))
                {
                    if (CheckIfLocaleVoiceIsAvailable(name.NameLanguage) && name.NameLanguage.ToLower().Equals(stationItem.PrimaryLocale.ToLower() ?? "jp"))
                    {
                        return name.Name;
                    }
                }
            }

            //if no native language names are available, check if theres a specific way to pronounce their name.

            if (!string.IsNullOrWhiteSpace(builtInArtist.NameSayAs))
                return builtInArtist.NameSayAs;

            return songMetadata.Artist; //fallback
        }

        private static async Task PlayAnnouncementAudioStreamAsync(SpeechSynthesisStream stream, VoiceMode voiceMode)
        {
            await await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
            {
                var source = MediaSource.CreateFromStream(stream, stream.ContentType);

                //media.AudioCategory = MediaPlayerAudioCategory.Speech;
                if (voiceMode == VoiceMode.Bluetooth)
                {
                    announcementMediaElement.AudioCategory = Windows.UI.Xaml.Media.AudioCategory.Media; //Speech is too low.
                }
                else if (voiceMode == VoiceMode.Headphones)
                {
                    announcementMediaElement.AudioCategory = Windows.UI.Xaml.Media.AudioCategory.Alerts;
                }

                //media.CommandManager.IsEnabled = false;
                announcementMediaElement.Volume = 1.0;

                announcementMediaElement.SetPlaybackSource(source);

                Task mediaOpenTask = announcementMediaElement.WaitForMediaOpenAsync();

                announcementMediaElement.Play();

                await mediaOpenTask;

                await Task.Delay((int)announcementMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds);

                source.Dispose();

                stream.Dispose();

                //media.Dispose();
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

            bool nativeVoiceAvailable = CheckIfLocaleVoiceIsAvailable(locale);

            if (index == 3 && nativeVoiceAvailable)
                index = 1;

            builder.AppendLine(@"<?xml version='1.0' encoding='ISO-8859-1'?>");
            builder.AppendLine(@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>");
            builder.AppendLine("<voice gender='female'>");

            Action<string> speakInEnglish = (text) =>
            {
                builder.AppendLine("<s>");
                builder.AppendLine("<prosody volume='x-loud'>");
                builder.AppendLine(text);
                builder.AppendLine("</prosody>");
                builder.AppendLine("</s>");
            };
            Action<string> speakInJapanese = (text) =>
            {
                //var voice = (japaneseVoiceAvailable ? japaneseFemaleVoice : SpeechSynthesizer.DefaultVoice);

                //builder.AppendLine("<voice xml:lang='" + voice.Language + "' name='" + voice.DisplayName + "'>");
                builder.AppendLine("<s>");
                builder.AppendLine("<voice" + (japaneseFemaleVoice != null ? " name='" + japaneseFemaleVoice.DisplayName + "'" : " gender='female'") + ">");
                builder.AppendLine("<prosody volume='x-loud'>");
                builder.AppendLine(text);
                builder.AppendLine("</prosody>");
                builder.AppendLine("</voice>");
                builder.AppendLine("</s>");
            };
            Action<string> speakInKorean = (text) =>
            {
                builder.AppendLine("<s>");
                builder.AppendLine("<voice" + (koreanFemaleVoice != null ? " name='" + koreanFemaleVoice.DisplayName + "'" : " gender='female'") + ">");
                builder.AppendLine("<prosody volume='x-loud'>");
                builder.AppendLine(text);
                builder.AppendLine("</prosody>");
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

        private static bool CheckIfLocaleVoiceIsAvailable(string locale)
        {
            bool nativeVoiceAvailable = false;
            switch (locale.ToLower().Trim())
            {
                case "jp":
                case "ja":
                    nativeVoiceAvailable = japaneseFemaleVoice != null;
                    break;
                case "kr":
                    nativeVoiceAvailable = koreanFemaleVoice != null;
                    break;
            }

            return nativeVoiceAvailable;
        }
    }

    public enum VoiceMode
    {
        Bluetooth,
        Headphones
    }
}
