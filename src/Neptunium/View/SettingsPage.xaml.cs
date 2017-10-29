using Crystal3;
using Crystal3.UI;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.SettingsPageViewModel))]
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private async void SelectBluetoothButton_Click(object sender, RoutedEventArgs e)
        {
            SelectBluetoothButton.IsEnabled = false;
            var device = await NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.SelectDeviceAsync(Window.Current.Bounds);
            if (device != null)
            {
                this.GetViewModel<SettingsPageViewModel>().SelectedBluetoothDeviceName = device.Name;
            }
            SelectBluetoothButton.IsEnabled = true;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Xbox)
            {
                //only show bluetooth settings on devices that have bluetooth.
                bluetoothPivot.IsEnabled = await NepApp.MediaPlayer.Bluetooth.DeviceCoordinator.HasBluetoothRadiosAsync();

                UpdateLockScreenSwitch.Visibility = Visibility.Visible;
            }
        }
    }
}
