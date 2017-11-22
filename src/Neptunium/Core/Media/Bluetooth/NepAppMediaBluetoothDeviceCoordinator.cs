using Crystal3.Core;
using Microsoft.HockeyApp;
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
using Windows.Storage;

namespace Neptunium.Core.Media.Bluetooth
{
    public class NepAppMediaBluetoothDeviceCoordinator
    {
        public const string SelectedBluetoothDeviceNameSettingsKey = "SelectedBluetoothDeviceName";

        public bool IsInitialized { get; private set; }
        public ReadOnlyObservableCollection<DeviceInformation> DetectedBluetoothDevices { get; private set; }

        private Radio btRadio = null;
        public string SelectedBluetoothDeviceId { get; private set; }
        public BluetoothDevice SelectedBluetoothDevice { get; private set; }
        public string SelectedBluetoothDeviceName { get; private set; }
        private SemaphoreSlim btRadioStateChangeLock = null;

        public bool IsBluetoothConnected { get; private set; }
        public event EventHandler<NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs> IsBluetoothConnectedChanged;

        internal NepAppMediaBluetoothDeviceCoordinator()
        {
        }

        internal async Task InitializeAsync()
        {
            if (IsInitialized) return;

            //there aren't any bluetooth radios so stop here.
            if (!await HasBluetoothRadiosAsync()) return;

            btRadio = (await Radio.GetRadiosAsync()).First(x => x.Kind == RadioKind.Bluetooth);

            btRadioStateChangeLock = new SemaphoreSlim(1);

            await App.Dispatcher.RunAsync(IUIDispatcherPriority.High, () =>
            {
                btRadio.StateChanged += BtRadio_StateChanged;
            });

            //Pull the selected bluetooth device from settings if it exists
            if (NepApp.Settings.ContainsSetting(AppSettings.SelectedBluetoothDevice))
            {
                await InitializeBluetoothDeviceFromSettingsAsync();

                if (NepApp.Settings.ContainsSetting(AppSettings.SelectedBluetoothDeviceName))
                {
                    SelectedBluetoothDeviceName = NepApp.Settings.GetSetting(AppSettings.SelectedBluetoothDeviceName) as string;
                }
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Updates the bluetooth connection state.
        /// </summary>
        /// <param name="state">Whether the bluetooth device is connected or not.</param>
        private void UpdateBluetoothState(bool state)
        {
            IsBluetoothConnected = state;

            IsBluetoothConnectedChanged?.Invoke(this, new NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs(state));
        }

        private async Task InitializeBluetoothDeviceFromSettingsAsync()
        {
            var deviceID = NepApp.Settings.GetSetting(AppSettings.SelectedBluetoothDevice) as string;

            if (!string.IsNullOrWhiteSpace(deviceID))
            {
                BluetoothDevice device = null;

                if (btRadio.State == RadioState.On)
                {
                    await Task.Delay(250); //wait before trying to create the bluetooth device.
                    device = await BluetoothDevice.FromIdAsync(deviceID);
                }

                SetBluetoothDevice(deviceID, device);
            }
        }

        public async Task<bool> HasBluetoothRadiosAsync()
        {
            return (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth).Count() > 0;
        }

        public async Task<bool> GetIfBluetoothIsOnAsync()
        {
            var BTradios = (await Radio.GetRadiosAsync()).Where(x => x.Kind == RadioKind.Bluetooth);

            return BTradios.Any(x => x.State == RadioState.On);
        }

        private void SetBluetoothDevice(string id, BluetoothDevice device)
        {
            if (SelectedBluetoothDevice != null)
            {
                SelectedBluetoothDevice.ConnectionStatusChanged -= BluetoothDevice_ConnectionStatusChanged;
                SelectedBluetoothDevice.Dispose();
            }

            if (device != null)
            {
                SelectedBluetoothDevice = device;

                NepApp.Settings.SetSetting(AppSettings.SelectedBluetoothDeviceName, device.Name);

                SelectedBluetoothDeviceName = device.Name;

                SelectedBluetoothDevice.ConnectionStatusChanged += BluetoothDevice_ConnectionStatusChanged;

                UpdateBluetoothState(SelectedBluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
            }
            else
            {
                UpdateBluetoothState(false);
            }

            SelectedBluetoothDeviceId = id;
        }

        private void BluetoothDevice_ConnectionStatusChanged(BluetoothDevice sender, object args)
        {
            UpdateBluetoothState(SelectedBluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
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
                else
                {
                    UpdateBluetoothState(SelectedBluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
                }
            }
            else if (sender.State == RadioState.Off || sender.State == RadioState.Disabled)
            {
                UpdateBluetoothState(false);
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
                    NepApp.Settings.SetSetting(AppSettings.SelectedBluetoothDevice, selection.Id);
                    NepApp.Settings.SetSetting(AppSettings.SelectedBluetoothDeviceName, selection.Name);

                    SelectedBluetoothDeviceName = selection.Name;

                    await InitializeBluetoothDeviceFromSettingsAsync();
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

            NepApp.Settings.SetSetting(AppSettings.SelectedBluetoothDevice, string.Empty);

            UpdateBluetoothState(false);
        }
    }
}
