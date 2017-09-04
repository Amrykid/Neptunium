using System;

namespace Neptunium.Core.Media.Bluetooth
{
    public class NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs: EventArgs
    {
        public bool IsConnected { get; private set; }

        public NepAppMediaBluetoothDeviceCoordinatorIsBluetoothConnectedChangedEventArgs(bool state)
        {
            IsConnected = state;
        }
    }
}