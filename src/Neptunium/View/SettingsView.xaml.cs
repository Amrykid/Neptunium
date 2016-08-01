using Crystal3;
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

#if RELEASE
            carModePivot.Visibility = CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile ? Visibility.Visible : Visibility.Collapsed;
#else
            carModePivot.Visibility = Visibility.Visible;
#endif
        }
    }
}
