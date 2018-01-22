using Crystal3;
using Crystal3.UI;
using Neptunium.ViewModel.Dialog;
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
    public sealed partial class StationInfoDialog : Page
    {
        public StationInfoDialog()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
                PinStationButton.Visibility = Visibility.Collapsed; //pinning isn't supported on Xbox.

            //Focus on the cancel button.
            CancelButton.Focus(FocusState.Programmatic);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem;

            this.GetViewModel<StationInfoDialogFragment>().PlayStreamCommand.Execute(item);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var item = ((Button)sender).DataContext;
            this.GetViewModel<StationInfoDialogFragment>().PlayStreamCommand.Execute(item);
        }
    }
}
