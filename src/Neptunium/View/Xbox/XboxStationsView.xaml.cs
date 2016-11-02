using Crystal3.Navigation;
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

namespace Neptunium.View.Xbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(StationsViewViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxStationsView : Page
    {
        public XboxStationsView()
        {
            this.InitializeComponent();
        }

        private void StationsListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            var appCommands = (App.Current.Resources["AppCommands"] as ApplicationCommands);
            var item = e.ClickedItem;

            appCommands.PlayStationCommand.Execute(item);
        }

        private void StationsListBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                try
                {
                    StationsListBox.Focus(FocusState.Programmatic);
                    StationsListBox.SelectedIndex = 0;
                }
                catch (Exception) { }
            }
        }
    }
}
