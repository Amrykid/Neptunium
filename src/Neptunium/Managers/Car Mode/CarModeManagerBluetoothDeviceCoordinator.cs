using Crystal3;
using Crystal3.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Storage;

namespace Neptunium.Managers.Car_Mode
{
    public class CarModeManagerBluetoothDeviceCoordinator
    {
        public const string SelectedCarDeviceNameSettingsKey = "SelectedCarDeviceName";

        public bool IsInitialized { get; private set; }
        public ReadOnlyObservableCollection<DeviceInformation> DetectedBluetoothDevices { get; private set; }

        private RadioAccessStatus radioAccess = RadioAccessStatus.Unspecified;
        private Radio btRadio = null; //Since this is a mobile only feature, it is safe to assume there is only 1 bluetooth radio.
        public string SelectedBluetoothDeviceId { get; private set; }
        public BluetoothDevice SelectedBluetoothDevice { get; private set; }
        public string SelectedBluetoothDeviceName { get; private set; }
        private SemaphoreSlim btRadioStateChangeLock = null;

        public IObservable<bool> BluetoothConnectionStatusChanged { get; private set; }
        protected BehaviorSubject<bool> bluetoothConnectionStatusSubject = null;

        internal CarModeManagerBluetoothDeviceCoordinator()
        {
        }

        internal async Task InitializeAsync()
        {
            if (IsInitialized) return;

            radioAccess = await Radio.RequestAccessAsync();

            //we're not allowed to access radios so stop here.
            if (radioAccess != RadioAccessStatus.Allowed) return;

            //there aren't any bluetooth radios so stop here.
            if (!await HasBluetoothRadiosAsync()) return;

            bluetoothConnectionStatusSubject = new BehaviorSubject<bool>(false);
            BluetoothConnectionStatusChanged = bluetoothConnectionStatusSubject;

            btRadio = (await Radio.GetRadiosAsync()).First(x => x.Kind == RadioKind.Bluetooth);

            btRadioStateChangeLock = new SemaphoreSlim(1);

            await App.Dispatcher.RunAsync(IUIDispatcherPriority.High, () =>
            {
                btRadio.StateChanged += BtRadio_StateChanged;
            });

            //Pull the selected bluetooth device from settings if it exists
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeManager.SelectedCarDevice))
            {
                await InitializeBluetoothDeviceFromSettingsAsync();

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDeviceNameSettingsKey))
                {
                    SelectedBluetoothDeviceName = ApplicationData.Current.LocalSettings.Values[SelectedCarDeviceNameSettingsKey] as string;
                }
            }

            IsInitialized = true;
        }

        private async Task InitializeBluetoothDeviceFromSettingsAsync()
        {
            var deviceID = ApplicationData.Current.LocalSettings.Values[CarModeManager.SelectedCarDevice] as string;

            if (!string.IsNullOrWhiteSpace(deviceID))
            {
                BluetoothDevice device = null;

                if (btRadio.State == RadioState.On)
                    device = await BluetoothDevice.FromIdAsync(deviceID);

                SetBluetoothDevice(deviceID, device);
            }
        }

        private async Task<bool> HasBluetoothRadiosAsync()
        {
            return (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth).Count() > 0;
        }

        private async Task<bool> GetIfBluetoothIsOnAsync()
        {
            if (radioAccess == RadioAccessStatus.Allowed)
            {
                var BTradios = (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth);

                return BTradios.Any(x => x.State == RadioState.On);
            }

            return false;
        }

        private void SetBluetoothDevice(string id, BluetoothDevice device)
        {
            if (SelectedBluetoothDevice != null)
            {
                SelectedBluetoothDevice.ConnectionStatusChanged -= BluetoothDevice_ConnectionStatusChanged;
            }

            if (device != null)
            {
                SelectedBluetoothDevice = device;

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SelectedCarDeviceNameSettingsKey))
                {
                    ApplicationData.Current.LocalSettings.Values[SelectedCarDeviceNameSettingsKey] = device.Name;
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values.Add(SelectedCarDeviceNameSettingsKey, device.Name);
                }
                SelectedBluetoothDeviceName = device.Name;

                SelectedBluetoothDevice.ConnectionStatusChanged += BluetoothDevice_ConnectionStatusChanged;

                bluetoothConnectionStatusSubject.OnNext(SelectedBluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
            }
            else
            {
                bluetoothConnectionStatusSubject.OnNext(false);
            }

            SelectedBluetoothDeviceId = id;
        }

        private void BluetoothDevice_ConnectionStatusChanged(BluetoothDevice sender, object args)
        {
            bluetoothConnectionStatusSubject.OnNext(SelectedBluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
        }

        private async void BtRadio_StateChanged(Radio sender, object args)
        {
            await btRadioStateChangeLock.WaitAsync();
            if (sender.State == RadioState.On)
            {
                if (SelectedBluetoothDevice == null)
                {
                    await InitializeBluetoothDeviceFromSettingsAsync();
                }
            }
            btRadioStateChangeLock.Release();
        }

        public async Task<DeviceInformation> SelectDeviceAsync(Windows.Foundation.Rect uiArea)
        {
            if (!IsInitialized) throw new InvalidOperationException();

            if (!await HasBluetoothRadiosAsync()) throw new InvalidOperationException();

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
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeManager.SelectedCarDevice))
                    {
                        ApplicationData.Current.LocalSettings.Values[CarModeManager.SelectedCarDevice] = selection.Id;

                        await InitializeBluetoothDeviceFromSettingsAsync();
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

        public void ClearDevice()
        {
            if (!IsInitialized) throw new InvalidOperationException();

            SetBluetoothDevice(null, null);

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(CarModeManager.SelectedCarDevice))
                ApplicationData.Current.LocalSettings.Values[CarModeManager.SelectedCarDevice] = string.Empty;

            bluetoothConnectionStatusSubject.OnNext(false);
        }
    }
}
