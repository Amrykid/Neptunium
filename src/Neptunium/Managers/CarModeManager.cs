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

        public static bool IsInitialized { get; private set; }

        public static bool IsInCarMode { get; private set; }

        public static ReadOnlyObservableCollection<DeviceInformation> DetectedBluetoothDevices { get; private set; }
        public static DeviceInformation SelectedDevice { get; private set; }
        public static bool ShouldAnnounceSongs { get; private set; }

        private static DeviceWatcher watcher = null;
        private static ObservableCollection<DeviceInformation> detectedDevices = new ObservableCollection<DeviceInformation>();
        private static SpeechSynthesizer speechSynth = new SpeechSynthesizer();
        private static VoiceInformation japaneseFemaleVoice = null;

        public static async void Initialize()
        {
            if (IsInitialized) return;

#if RELESE
            if (Crystal3.CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Mobile) return;
#endif

            //creates a AQS filter string for watching for PAIRED bluetooth devices.
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            //creates a device watcher which will detect paired bluetooth devices and watches their connection status,
            watcher = DeviceInformation.CreateWatcher(selector, new List<string> { "System.Devices.Aep.IsConnected" }, DeviceInformationKind.Device);

            watcher.Added += Watcher_Added;
            watcher.Removed += Watcher_Removed;
            watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
            watcher.Updated += Watcher_Updated;

            DetectedBluetoothDevices = new ReadOnlyObservableCollection<DeviceInformation>(detectedDevices);

            //Pull the selected bluetooth device from settings if it exists
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDevice))
            {
                var deviceID = ApplicationData.Current.LocalSettings.Values[SelectedCarDevice] as string;

                if (!string.IsNullOrWhiteSpace(deviceID))
                {
                    SelectedDevice = await DeviceInformation.CreateFromIdAsync(deviceID, new List<string> { "System.Devices.Aep.IsConnected" }, DeviceInformationKind.Device);

                    if (SelectedDevice != null)
                    {
                        try
                        {
                            bool isConnected = (bool)SelectedDevice.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                            SetCarModeStatus(isConnected);
                        }
                        catch (Exception) { }
                    }
                }
            }
            //Create a settings entry for the selected bluetooth device if it doesn't exist
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeAnnounceSongs))
                ApplicationData.Current.LocalSettings.Values.Add(CarModeAnnounceSongs, false);

            ShouldAnnounceSongs = (bool)ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs];

            StationMediaPlayer.MetadataChanged += StationMediaPlayer_MetadataChanged;

            japaneseFemaleVoice = SpeechSynthesizer.AllVoices.FirstOrDefault(x => 
                x.Language.ToLower().StartsWith("ja") && x.Gender == VoiceGender.Female);


            watcher.Start();

            IsInitialized = true;
        }

        private static async void StationMediaPlayer_MetadataChanged(object sender, MediaSourceStream.ShoutcastMediaSourceStreamMetadataChangedEventArgs e)
        {
            if (ShouldAnnounceSongs && IsInCarMode)
            {
                double initialVolume = StationMediaPlayer.Volume;
                await FadeVolumeDownToAsync(.1); //lower the volume of the song so that the announcement can be heard.

                if (japaneseFemaleVoice != null)
                    speechSynth.Voice = japaneseFemaleVoice;

                var nowPlayingSpeech = string.Format(GetRandomNowPlayingText(), e.Artist, e.Title);
                var stream = await speechSynth.SynthesizeTextToStreamAsync(nowPlayingSpeech);

                await CrystalApplication.Dispatcher.RunWhenIdleAsync(async () =>
                {
                    var media = new MediaElement();

                    media.Volume = 1.0;
                    media.SetSource(stream, "");
                    media.Play();

                    await Task.Delay(nowPlayingSpeech.Length * 155);

                    stream.Dispose();

                    await FadeVolumeUpToAsync(initialVolume); //raise the volume back up
                });


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

        private static async Task FadeVolumeDownToAsync(double value)
        {
            var initial = StationMediaPlayer.Volume;
            for (double x = initial; x > value; x -= .1)
            {
                await Task.Delay(50);
                StationMediaPlayer.Volume = x;
            }
        }
        private static async Task FadeVolumeUpToAsync(double value)
        {
            var initial = StationMediaPlayer.Volume;
            for (double x = initial; x < value; x += .1)
            {
                await Task.Delay(50);
                StationMediaPlayer.Volume = x;
            }
        }

        private static void SetCarModeStatus(bool isConnected)
        {
            IsInCarMode = isConnected;

            if (CarModeManagerCarModeStatusChanged != null)
                CarModeManagerCarModeStatusChanged(null, new CarModeManagerCarModeStatusChangedEventArgs(isConnected));
        }

        public static void SetShouldAnnounceSongs(bool value)
        {
            ShouldAnnounceSongs = value;

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeAnnounceSongs))
                ApplicationData.Current.LocalSettings.Values.Add(CarModeAnnounceSongs, value);
            else
                ApplicationData.Current.LocalSettings.Values[CarModeAnnounceSongs] = value;
        }

        public static async Task SelectDeviceAsync(Windows.Foundation.Rect uiArea)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            DevicePicker picker = new DevicePicker();
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
                else
                    ApplicationData.Current.LocalSettings.Values.Add(SelectedCarDevice, SelectedDevice.Id);
            }
        }

        public static event EventHandler<CarModeManagerCarModeStatusChangedEventArgs> CarModeManagerCarModeStatusChanged;

        #region Device Watcher Stuff
        private static void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (detectedDevices.Any(x => x.Id == args.Id))
            {
                var device = detectedDevices.First(x => x.Id == args.Id);
                device.Update(args);

                if (device.Id == SelectedDevice?.Id)
                {
                    bool isConnected = (bool)device.Properties.FirstOrDefault(p => p.Key == "System.Devices.Aep.IsConnected").Value;
                    SetCarModeStatus(isConnected);
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
