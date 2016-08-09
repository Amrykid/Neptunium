using Crystal3;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

        #region Options
        public static DeviceInformation SelectedDevice { get; private set; }
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

        public static async void Initialize()
        {
            if (IsInitialized) return;

#if RELESE
            if (Crystal3.CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Mobile) return;
#endif

            //creates a AQS filter string for watching for PAIRED bluetooth devices.
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            //creates a device watcher which will detect paired bluetooth devices and watches their connection status,
            watcher = DeviceInformation.CreateWatcher(selector, GetWantedProperties());

            watcher.Added += Watcher_Added;
            watcher.Removed += Watcher_Removed;
            watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
            watcher.Updated += Watcher_Updated;

            DetectedBluetoothDevices = new ReadOnlyObservableCollection<DeviceInformation>(detectedDevices);

            radioAccess = await await App.Dispatcher.RunWhenIdleAsync(() =>
            {
                return Radio.RequestAccessAsync();
            });

            if (radioAccess == RadioAccessStatus.Allowed)
            {
                var BTradios = (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth);
                foreach(var btRadio in BTradios)
                    btRadio.StateChanged += BtRadio_StateChanged;
            }


                //Pull the selected bluetooth device from settings if it exists
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
            {
                var deviceID = ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] as string;

                if (!string.IsNullOrWhiteSpace(deviceID))
                {
                    SelectedDevice = await DeviceInformation.CreateFromIdAsync(deviceID, GetWantedProperties());

                    if (SelectedDevice != null)
                    {
                        if (watcher.Status == DeviceWatcherStatus.Stopped || watcher.Status == DeviceWatcherStatus.Created)
                            watcher.Start();

                        bool isConnected = (bool)SelectedDevice.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                        SetCarModeStatus(isConnected && await GetIfBluetoothIsOnAsync());
                    }
                }
            }

            CreateSettings();

            ShouldAnnounceSongs = (bool)ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs];
            ShouldUseJapaneseVoice = (bool)ApplicationData.Current.LocalSettings.Values[UseJapaneseVoiceForAnnouncements];

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;

            japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x =>
                x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female);


            IsInitialized = true;
        }

        private static async void BtRadio_StateChanged(Radio sender, object args)
        {
            if (SelectedDevice != null)
            {
                bool isConnected = (bool)SelectedDevice.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                SetCarModeStatus(isConnected && await GetIfBluetoothIsOnAsync());
            }
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

        private static IEnumerable<string> GetWantedProperties()
        {
            return new List<string> { "System.Devices.Connected", "System.Devices.Aep.IsConnected" };
        }

        private static async void StationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (ShouldAnnounceSongs && IsInCarMode)
            {
                double initialVolume = StationMediaPlayer.Volume;
                await StationMediaPlayer.FadeVolumeDownToAsync(.05); //lower the volume of the song so that the announcement can be heard.

                if (japaneseFemaleVoice != null && ShouldUseJapaneseVoice)
                    speechSynth.Voice = japaneseFemaleVoice;
                else
                    speechSynth.Voice = SpeechSynthesizer.DefaultVoice;

                var nowPlayingSpeech = string.Format(GetRandomNowPlayingText(), e.Artist, e.Title);
                var stream = await speechSynth.SynthesizeTextToStreamAsync(nowPlayingSpeech);

                await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
                {
                    var media = new MediaElement();

                    media.Volume = 1.0;
                    media.AudioCategory = Windows.UI.Xaml.Media.AudioCategory.Alerts;
                    media.SetSource(stream, stream.ContentType);
                    media.Play();

                    await Task.Delay(nowPlayingSpeech.Length * 155);

                    stream.Dispose();

                    await StationMediaPlayer.FadeVolumeUpToAsync(initialVolume); //raise the volume back up
                });


            }
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

        private static string GetRandomNowPlayingText()
        {
            var phrases = new string[] {
                "Now Playing: {1} by {0}",
                "And Now: {1} by {0}",
                "Playing: {1} by {0}",
                "君が{0}の{1}を聞いています"
            };
            var randomizer = new Random(DateTime.Now.Millisecond);
            var index = randomizer.Next(0, phrases.Length - 1);

            return phrases[index];
        }

        public static async Task SelectDeviceAsync(Windows.Foundation.Rect uiArea)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            DevicePicker picker = new DevicePicker();

            foreach (var prop in GetWantedProperties())
                picker.RequestedProperties.Add(prop);

            picker.Filter.SupportedDeviceClasses.Add(DeviceClass.AudioRender);
            picker.Filter.SupportedDeviceSelectors.Add(BluetoothDevice.GetDeviceSelectorFromPairingState(true));

            SelectedDevice = await picker.PickSingleDeviceAsync(uiArea);

            if (SelectedDevice == null)
            {
            }
            else
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                    ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] = SelectedDevice.Id;

                if (watcher.Status == DeviceWatcherStatus.Stopped || watcher.Status == DeviceWatcherStatus.Stopping || watcher.Status == DeviceWatcherStatus.Created)
                {
                    if (watcher.Status == DeviceWatcherStatus.Stopping)
                    {
                        TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                        TypedEventHandler<DeviceWatcher, object> handler = null;
                        handler = new TypedEventHandler<DeviceWatcher, object>((w, o) =>
                        {
                            watcher.Stopped -= handler;
                            tcs.SetResult(null);
                        });
                        watcher.Stopped += handler;

                        await tcs.Task;
                    }

                    if (watcher.Status == DeviceWatcherStatus.Stopped || watcher.Status == DeviceWatcherStatus.Created)
                        watcher.Start();
                }

                bool isConnected = (bool)SelectedDevice.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                SetCarModeStatus(isConnected && await GetIfBluetoothIsOnAsync());
            }
        }

        public static void ClearDevice()
        {
            if (!IsInitialized) throw new InvalidOperationException();

            SelectedDevice = null;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
                ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] = string.Empty;

            if (watcher.Status != DeviceWatcherStatus.Stopped && watcher.Status != DeviceWatcherStatus.Stopping)
                watcher.Stop();

            SetCarModeStatus(false);
        }

        public static event EventHandler<CarModeManagerCarModeStatusChangedEventArgs> CarModeManagerCarModeStatusChanged;

        #region Device Watcher Stuff
        private static async void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (detectedDevices.Any(x => x.Id == args.Id))
            {
                var device = detectedDevices.FirstOrDefault(x => x.Id == args.Id);
                if (device != null)
                {
                    device.Update(args);

                    if (device.Id == SelectedDevice?.Id)
                    {
                        SelectedDevice = device;
                        try
                        {
                            bool isConnected = (bool)device.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                            SetCarModeStatus(isConnected && await GetIfBluetoothIsOnAsync());
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        private static void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {

        }

        private static void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (detectedDevices.Any(x => x.Id == args.Id))
                detectedDevices.Remove(detectedDevices.First(x => x.Id == args.Id));
        }

        private static void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            detectedDevices.Add(args);
        }
        #endregion
    }
}
