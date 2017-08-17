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
    [Crystal3.Navigation.NavigationViewModel(typeof(StationsPageViewModel), Crystal3.Navigation.NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxStationsPage : Page, IXboxInputPage
    {
        public XboxStationsPage()
        {
            this.InitializeComponent();
        }

        private GridViewItem focusedItem = null;
        public void PreserveFocus()
        {
            var selection = stationsGridView.SelectedItem;
            focusedItem = (GridViewItem)stationsGridView.ContainerFromItem(selection);
        }

        public void RestoreFocus()
        {
            if (focusedItem != null)
            {
                focusedItem.Focus(FocusState.Keyboard);
                focusedItem = null;
            }
        }
    }
}
