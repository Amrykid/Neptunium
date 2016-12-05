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
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Neptunium.Managers
{
    public static class CarModeManager
    {
        public const string SelectedCarDevice = "SelectedCarDevice";
        public const string CarModeAnnounceSongs = "CarModeAnnounceSongs";
        public const string UseJapaneseVoiceForAnnouncements = "UseJapaneseVoiceForAnnouncements";

        public static bool IsInitialized { get; private set; }

        public static bool IsInCarMode { get; private set; }

        public static ReadOnlyObservableCollection<DeviceInformation> DetectedBluetoothDevices { get; private set; }

        private static DeviceWatcher watcher = null;
        private static ObservableCollection<DeviceInformation> detectedDevices = new ObservableCollection<DeviceInformation>();
        private static SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private static VoiceInformation japaneseFemaleVoice = null;
        private static RadioAccessStatus radioAccess = RadioAccessStatus.Unspecified;
        private static Radio btRadio = null; //Since this is a mobile only feature, it is safe to assume there is only 1 bluetooth radio.

        private static string lastPlayedSongMetadata = null;

        #region Options
        public static DeviceInformation SelectedDevice { get; private set; }
        private static BluetoothDevice SelectedDeviceObj { get; set; }
        public static bool ShouldAnnounceSongs { get; private set; }
        public static bool ShouldUseJapaneseVoice { get; private set; }

        private static void CreateSettings()
        {
            //Create settings entries
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeAnnounceSongs))
                ApplicationData.Current.LocalSettings.Values.Add(CarModeAnnounceSongs, false);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                ApplicationData.Current.LocalSettings.Values.Add(SelectedCarDevice, string.Empty);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(UseJapaneseVoiceForAnnouncements))
                ApplicationData.Current.LocalSettings.Values.Add(UseJapaneseVoiceForAnnouncements, false);
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

            if (Crystal3.CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Mobile) return;

            radioAccess = await await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                return Radio.RequestAccessAsync();
            });

            //we're not allowed to access radios so stop here.
            if (radioAccess != RadioAccessStatus.Allowed) return;

            //there aren't any bluetooth radios so stop here.
            if ((await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth).Count() == 0) return;

            //Pull the selected bluetooth device from settings if it exists
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
            {
                var deviceID = ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] as string;

                if (!string.IsNullOrWhiteSpace(deviceID))
                {
                    SelectedDevice = await DeviceInformation.CreateFromIdAsync(deviceID);

                    if (SelectedDevice != null)
                    {
                        if (radioAccess == RadioAccessStatus.Allowed)
                        {
                            btRadio = (await Radio.GetRadiosAsync()).First(x => x.Kind == RadioKind.Bluetooth);

                            if (btRadio.State == RadioState.On)
                            {
                                SelectedDeviceObj = await BluetoothDevice.FromIdAsync(SelectedDevice.Id);

                                SelectedDeviceObj.ConnectionStatusChanged += SelectedDeviceObj_ConnectionStatusChanged;

                                SetCarModeStatus(SelectedDeviceObj.ConnectionStatus == BluetoothConnectionStatus.Connected);
                            }
                            else
                            {
                                btRadio.StateChanged += BtRadio_StateChanged;
                            }
                        }
                    }
                }
            }


            CreateSettings();

            ShouldAnnounceSongs = (bool)ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs];
            ShouldUseJapaneseVoice = (bool)ApplicationData.Current.LocalSettings.Values[UseJapaneseVoiceForAnnouncements];

            SongManager.PreSongChanged += SongManager_PreSongChanged;
            StationMediaPlayer.BackgroundAudioReconnecting += StationMediaPlayer_BackgroundAudioReconnecting;

            japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female && x.DisplayName.Contains("Haruka"));

            IsInitialized = true;
        }

        private static async void SongManager_PreSongChanged(object sender, SongManagerSongChangedEventArgs e)
        {
            if (ShouldAnnounceSongs && IsInCarMode)
            {
                if (lastPlayedSongMetadata == e.Metadata.Track) return;

                double initialVolume = StationMediaPlayer.Volume;

                var nowPlayingSsmlData = GenerateSongAnnouncementSsml(e.Metadata.Artist, e.Metadata.Track, japaneseFemaleVoice != null && ShouldUseJapaneseVoice);
                var stream = await speechSynth.SynthesizeSsmlToStreamAsync(nowPlayingSsmlData);

                await StationMediaPlayer.FadeVolumeDownToAsync(0.1);

                await PlayAnnouncementAudioStreamAsync(stream);

                await StationMediaPlayer.FadeVolumeUpToAsync(initialVolume);

            }
        }

        private static void SelectedDeviceObj_ConnectionStatusChanged(BluetoothDevice sender, object args)
        {
            SetCarModeStatus(sender.ConnectionStatus == BluetoothConnectionStatus.Connected);
        }

        private static SemaphoreSlim btRadioStateChangeLock = new SemaphoreSlim(1);
        private static async void BtRadio_StateChanged(Radio sender, object args)
        {
            await btRadioStateChangeLock.WaitAsync();
            await App.Dispatcher.RunAsync(() =>
            {
                btRadio.StateChanged -= BtRadio_StateChanged; //event is fired twice for some reason so we're trying to throttle it.
            });

            if (sender.State == RadioState.On)
            {
                if (SelectedDevice != null && SelectedDeviceObj == null)
                {
                    //lazy initialize this since it cannot be created when bluetooth is off.
                    SelectedDeviceObj = await BluetoothDevice.FromIdAsync(SelectedDevice.Id);

                    SelectedDeviceObj.ConnectionStatusChanged += SelectedDeviceObj_ConnectionStatusChanged;

                    SetCarModeStatus(SelectedDeviceObj.ConnectionStatus == BluetoothConnectionStatus.Connected);

                    //todo check if re-enabling bluetooth after disabling it after this initialization still allows us to detect status changes
                }
            }

            btRadioStateChangeLock.Release();
        }

        private static async Task<bool> GetIfBluetoothIsOnAsync()
        {
            if (radioAccess == RadioAccessStatus.Allowed)
            {
                var BTradios = (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth);

                return BTradios.Any(x => x.State == RadioState.On);
            }

            return false;
        }

        private static async void StationMediaPlayer_BackgroundAudioReconnecting(object sender, EventArgs e)
        {
            if (ShouldAnnounceSongs && IsInCarMode)
            {
                double initialVolume = StationMediaPlayer.Volume;

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
                var media = new MediaElement();

                media.Volume = 1.0;
                media.AudioCategory = Windows.UI.Xaml.Media.AudioCategory.Alerts; //setting this to alerts automatically fades out music
                media.SetSource(stream, stream.ContentType);

                Task mediaOpenTask = media.WaitForMediaOpenAsync();

                media.Play();

                await mediaOpenTask;

                await Task.Delay((int)media.NaturalDuration.TimeSpan.TotalMilliseconds);

                stream.Dispose();
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

        public static async Task<DeviceInformation> SelectDeviceAsync(Windows.Foundation.Rect uiArea)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            DevicePicker picker = new DevicePicker();

            picker.Filter.SupportedDeviceClasses.Add(DeviceClass.AudioRender);
            picker.Filter.SupportedDeviceSelectors.Add(
                BluetoothDevice.GetDeviceSelectorFromClassOfDevice(
                    BluetoothClassOfDevice.FromParts(BluetoothMajorClass.AudioVideo,
                        BluetoothMinorClass.AudioVideoCarAudio | BluetoothMinorClass.AudioVideoHeadphones | BluetoothMinorClass.AudioVideoPortableAudio,
                        BluetoothServiceCapabilities.AudioService)));

            picker.Filter.SupportedDeviceSelectors.Add(BluetoothDevice.GetDeviceSelectorFromPairingState(true));

            var selection = await picker.PickSingleDeviceAsync(uiArea);

            try
            {
                if (selection != null)
                {
                    if (SelectedDeviceObj != null)
                        SelectedDeviceObj.ConnectionStatusChanged -= SelectedDeviceObj_ConnectionStatusChanged;

                    SelectedDevice = selection;

                    SelectedDeviceObj = await BluetoothDevice.FromIdAsync(SelectedDevice.Id);

                    if (SelectedDeviceObj != null)
                    {
                        SelectedDeviceObj.ConnectionStatusChanged += SelectedDeviceObj_ConnectionStatusChanged;

                        if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                            ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] = SelectedDevice.Id;

                        SetCarModeStatus(SelectedDeviceObj.ConnectionStatus == BluetoothConnectionStatus.Connected);
                    }
                }
            }
            catch (Exception ex)
            {
#if !DEBUG
                HockeyClient.Current.TrackException(ex);
#endif
            }

            return selection;
        }

        public static void ClearDevice()
        {
            if (!IsInitialized) throw new InvalidOperationException();

            SelectedDevice = null;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] = string.Empty;

            //if (watcher.Status != DeviceWatcherStatus.Stopped && watcher.Status != DeviceWatcherStatus.Stopping)
            //    watcher.Stop();

            SetCarModeStatus(false);
        }

        public static event EventHandler<CarModeManagerCarModeStatusChangedEventArgs> CarModeManagerCarModeStatusChanged;
    }
}
