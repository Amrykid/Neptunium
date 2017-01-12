using Crystal3;
using Microsoft.HockeyApp;
using Neptunium.Managers.Songs;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Neptunium.Managers.Car_Mode
{
    public static class CarModeManager
    {
        public const string SelectedCarDevice = "SelectedCarDevice";
        public const string CarModeAnnounceSongs = "CarModeAnnounceSongs";
        public const string UseJapaneseVoiceForAnnouncements = "UseJapaneseVoiceForAnnouncements";
        public const string SongAnnouncementFrequency = nameof(SongAnnouncementFrequency);

        public static bool IsInitialized { get; private set; }

        public static bool IsInCarMode { get; private set; }

        private static ObservableCollection<DeviceInformation> detectedDevices = new ObservableCollection<DeviceInformation>();
        private static SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private static VoiceInformation japaneseFemaleVoice = null;
        public static CarModeManagerBluetoothDeviceCoordinator BluetoothCoordinator { get; private set; }

        private static SongMetadata lastPlayedSongMetadata = null;

        private static SemaphoreSlim songAnouncementLock = new SemaphoreSlim(1);

        /// <summary>
        /// Used for managing when we should announce songs. Mainly for guarding against announcing the current song when we first connect to a station since its a bit jarring.
        /// </summary>
        private static bool canAnnounce = false;

        #region Options
        public static bool ShouldAnnounceSongs { get; private set; }
        public static bool ShouldUseJapaneseVoice { get; private set; }
        public static CarModeManagerSongAnnouncementFrequency AnnouncementFrequency { get; private set; }

        private static void CreateSettings()
        {
            //Create settings entries
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeAnnounceSongs))
                ApplicationData.Current.LocalSettings.Values.Add(CarModeAnnounceSongs, false);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                ApplicationData.Current.LocalSettings.Values.Add(SelectedCarDevice, string.Empty);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(UseJapaneseVoiceForAnnouncements))
                ApplicationData.Current.LocalSettings.Values.Add(UseJapaneseVoiceForAnnouncements, false);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(SongAnnouncementFrequency))
                ApplicationData.Current.LocalSettings.Values.Add(SongAnnouncementFrequency, 
                    Enum.GetName(typeof(CarModeManagerSongAnnouncementFrequency), CarModeManagerSongAnnouncementFrequency.Everytime));
        }

        public static void SetAnnouncementFrequency(CarModeManagerSongAnnouncementFrequency value)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            AnnouncementFrequency = value;

            ApplicationData.Current.LocalSettings.Values[SongAnnouncementFrequency] = Enum.GetName(typeof(CarModeManagerSongAnnouncementFrequency), value);
        }

        public static void SetShouldAnnounceSongs(bool value)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            ShouldAnnounceSongs = value;

            ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs] = value;
        }

        public static void SetShouldUseJapaneseVoice(bool value)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            ShouldUseJapaneseVoice = value;

            ApplicationData.Current.LocalSettings.Values[UseJapaneseVoiceForAnnouncements] = value;
        }
        #endregion

        public static async Task InitializeAsync()
        {
            if (IsInitialized) return;

#if !DEBUG
            if (Crystal3.CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Mobile) return;
#endif

            BluetoothCoordinator = new CarModeManagerBluetoothDeviceCoordinator();
            await BluetoothCoordinator.InitializeAsync();
            BluetoothCoordinator.BluetoothConnectionStatusChanged.Subscribe(status =>
            {
                SetCarModeStatus(status);
            });

            CreateSettings();

            ShouldAnnounceSongs = (bool)ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs];
            ShouldUseJapaneseVoice = (bool)ApplicationData.Current.LocalSettings.Values[UseJapaneseVoiceForAnnouncements];
            AnnouncementFrequency = (CarModeManagerSongAnnouncementFrequency)Enum.Parse(typeof(CarModeManagerSongAnnouncementFrequency), 
                ApplicationData.Current.LocalSettings.Values[SongAnnouncementFrequency].ToString());

            SongManager.PreSongChanged += SongManager_PreSongChanged;
            StationMediaPlayer.BackgroundAudioReconnecting += StationMediaPlayer_BackgroundAudioReconnecting;
            StationMediaPlayer.CurrentStationChanged += StationMediaPlayer_CurrentStationChanged;

            japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female && x.DisplayName.Contains("Haruka"));

            IsInitialized = true;
        }

        private static void StationMediaPlayer_CurrentStationChanged(object sender, EventArgs e)
        {
            canAnnounce = false; //make it so that we don't immediately try to announce the first song on a station when we change.
        }

        private static async void SongManager_PreSongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            if (!canAnnounce)
            {
                //don't announce the current song when we first connect.
                canAnnounce = true;
                return;
            }

            if (e.IsUnknown || e.Metadata is UnknownSongMetadata) return;

            if (ShouldAnnounceSongs && IsInCarMode)
            {
                if (!ShouldDoAnnouncementBecauseDiceRollBasedOnFrequency()) return;

                await songAnouncementLock.WaitAsync();

                if (lastPlayedSongMetadata != e.Metadata)
                {
                    double initialVolume = 0.0;

                    try
                    {
                        initialVolume = StationMediaPlayer.Volume;
                    }
                    catch (InvalidOperationException)
                    {
                        songAnouncementLock.Release();
                        return;
                    }

                    var nowPlayingSsmlData = GenerateSongAnnouncementSsml(e.Metadata.Artist, e.Metadata.Track, japaneseFemaleVoice != null && ShouldUseJapaneseVoice);
                    var stream = await speechSynth.SynthesizeSsmlToStreamAsync(nowPlayingSsmlData);

                    bool shouldFade = initialVolume >= 0.1;

                    if (shouldFade)
                        await StationMediaPlayer.FadeVolumeDownToAsync(0.1);

                    await PlayAnnouncementAudioStreamAsync(stream);

                    if (shouldFade)
                        await StationMediaPlayer.FadeVolumeUpToAsync(initialVolume);

                    lastPlayedSongMetadata = e.Metadata;
                }

                songAnouncementLock.Release();
            }
        }

        private static bool ShouldDoAnnouncementBecauseDiceRollBasedOnFrequency()
        {
            switch(AnnouncementFrequency)
            {
                case CarModeManagerSongAnnouncementFrequency.Everytime:
                    return true;
                case CarModeManagerSongAnnouncementFrequency.Sometimes:
                    return new Random(DateTime.Now.Millisecond).Next(0, 101) <= 50; //50%
                case CarModeManagerSongAnnouncementFrequency.Occasionally:
                    return new Random(DateTime.Now.Millisecond).Next(0, 101) <= 33; //33%
            }

            return true;
        }

        private static async void StationMediaPlayer_BackgroundAudioReconnecting(object sender, EventArgs e)
        {
            if (ShouldAnnounceSongs && IsInCarMode)
            {
                double initialVolume = 0.0;
                try
                {
                    initialVolume = StationMediaPlayer.Volume;
                }
                catch (Exception)
                {
                    initialVolume = 1.0;
                }

                var englishVoice = SpeechSynthesizer.AllVoices.Where(x => x.Language.ToLower().StartsWith("en")).First(x => x.Gender == VoiceGender.Female);
                speechSynth.Voice = englishVoice;

                var stream = await speechSynth.SynthesizeTextToStreamAsync("Reconnecting... One moment please.");

                await PlayAnnouncementAudioStreamAsync(stream);
            }
        }

        private static async Task PlayAnnouncementAudioStreamAsync(SpeechSynthesisStream stream)
        {
            await await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
            {
                var source = MediaSource.CreateFromStream(stream, stream.ContentType);

                var media = new MediaPlayer();

                media.CommandManager.IsEnabled = false;
                media.Volume = 1.0;
                media.AudioCategory = MediaPlayerAudioCategory.Speech;
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

        private static string GenerateSongAnnouncementSsml(string artist, string title, bool japaneseVoiceAvailable)
        {
            StringBuilder builder = new StringBuilder();

            var englishVoice = SpeechSynthesizer.AllVoices.Where(x => x.Language.ToLower().StartsWith("en")).First(x => x.Gender == VoiceGender.Female);

            var phrases = new string[] {
                "Now Playing: {1} by {0}",
                "And Now: {1} by {0}",
                "Playing: {1} by {0}",
                "君が{0}の{1}を聞いています"
            };
            var randomizer = new Random(DateTime.Now.Millisecond);
            var index = randomizer.Next(0, phrases.Length - 1);

            var phrase = phrases[index];

            if (index == 3 && !japaneseVoiceAvailable)
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
                builder.AppendLine("<voice" + (japaneseVoiceAvailable ? " name='" + japaneseFemaleVoice.DisplayName + "'" : " gender='female'") + ">");

                builder.AppendLine(text);
                builder.AppendLine("</voice>");
                builder.AppendLine("</s>");
            };

            switch (index)
            {
                case 0:
                    speakInEnglish("Now Playing:");

                    speakInJapanese(title);

                    speakInEnglish("By");

                    speakInJapanese(artist);

                    break;
                case 1:
                    speakInEnglish("And Now:");

                    speakInJapanese(title);

                    speakInEnglish("By");

                    speakInJapanese(artist);

                    break;
                case 2:
                    speakInEnglish("Playing:");

                    speakInJapanese(title);

                    speakInEnglish("By");

                    speakInJapanese(artist);

                    break;
                case 3:
                    speakInJapanese(string.Format(phrase, artist, title));
                    break;
            }

            builder.AppendLine("</voice>");
            builder.AppendLine("</speak>");

            return builder.ToString();
        }

        private static void SetCarModeStatus(bool isConnected)
        {
            if (isConnected != IsInCarMode)
            {
                IsInCarMode = isConnected;

                if (CarModeManagerCarModeStatusChanged != null)
                    CarModeManagerCarModeStatusChanged(null, new CarModeManagerCarModeStatusChangedEventArgs(isConnected));
            }
        }

        public static event EventHandler<CarModeManagerCarModeStatusChangedEventArgs> CarModeManagerCarModeStatusChanged;
    }
}
