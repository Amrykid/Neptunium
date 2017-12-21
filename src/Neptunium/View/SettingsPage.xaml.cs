using Crystal3;
using Crystal3.UI;
using Neptunium.Glue;
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
    public sealed partial class SettingsPage : Page, IXboxInputPage
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
            else
            {
                //force focus on Xbox.
                RootPivot.Focus(FocusState.Keyboard);
            }
        }

        private async void SetFallBackLockScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file

                //todo check if less or equal to 2MB on mobile before proceeding.

                await NepApp.UI.LockScreen.SetFallbackImageAsync(file);
            }
            else
            {
                //cancelled
            }
        }

        public void SetLeftFocus(UIElement elementToTheLeft)
        {
            
        }

        public void SetRightFocus(UIElement elementToTheRight)
        {
            
        }

        public void SetTopFocus(UIElement elementAbove)
        {
            
        }

        public void SetBottomFocus(UIElement elementBelow)
        {
            
        }

        public void RestoreFocus()
        {
            
        }

        public void PreserveFocus()
        {
            
        }
    }
}
