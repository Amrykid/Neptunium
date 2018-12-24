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

namespace Neptunium.View.Dialog
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StationHandoffDialog : Page
    {
        public StationHandoffDialog()
        {
            this.InitializeComponent();
        }

        private void HandoffListButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                this.GetViewModel<StationHandoffDialogFragment>().HandOffCommand.Execute(e.ClickedItem);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Focus on the cancel button.
            CancelButton.Focus(FocusState.Programmatic);
        }
    }
}
