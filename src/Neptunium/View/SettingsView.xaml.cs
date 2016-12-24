using Crystal3;
using Neptunium.Managers;
using Neptunium.Managers.Car_Mode;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(SettingsViewViewModel))]
    public sealed partial class SettingsView : Page
    {
        public SettingsView()
        {
            this.InitializeComponent();


#if !DEBUG
            if (CrystalApplication.GetDevicePlatform() != Crystal3.Core.Platform.Mobile)
                mainPivot.Items.Remove(carModePivotItem);
#endif

            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
#if DEBUG
                if (Crystal3.CrystalApplication.GetCurrentAsCrystalApplication().Options.OverridePlatformDetection)
                    VisualStateManager.GoToState(this, XboxVisualState.Name, true);
#endif
            }
        }

        private void UpdateCarModeStatusIndicator(bool isInCarMode)
        {
            carModeStatusIndicatorRun.Text = isInCarMode ? "" : "";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                UpdateCarModeStatusIndicator(CarModeManager.IsInCarMode);

                CarModeManager.CarModeManagerCarModeStatusChanged += CarModeManager_CarModeManagerCarModeStatusChanged;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                CarModeManager.CarModeManagerCarModeStatusChanged -= CarModeManager_CarModeManagerCarModeStatusChanged;
            }

            base.OnNavigatingFrom(e);
        }

        private void CarModeManager_CarModeManagerCarModeStatusChanged(object sender, CarModeManagerCarModeStatusChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                UpdateCarModeStatusIndicator(e.IsInCarMode);
            });
        }
    }
}
