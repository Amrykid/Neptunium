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
    [Crystal3.Navigation.NavigationViewModel(typeof(SongHistoryPageViewModel))]
    public sealed partial class SongHistoryPage : Page, Neptunium.Glue.IXboxInputPage
    {
        public SongHistoryPage()
        {
            this.InitializeComponent();
        }

        private ListViewItem focusedItem = null;
        public void PreserveFocus()
        {
            var selection = SongHistoryListView.SelectedItem;
            if (selection != null)
            {
                focusedItem = (ListViewItem)SongHistoryListView.ContainerFromItem(selection);
            }
        }

        public void RestoreFocus()
        {
            if (focusedItem != null)
            {
                focusedItem.Focus(FocusState.Keyboard);
                focusedItem = null;
            }
        }

        public void SetBottomFocus(UIElement elementBelow)
        {
            
        }

        public void SetLeftFocus(UIElement elementToTheLeft)
        {
            SongHistoryListView.XYFocusLeft = elementToTheLeft;
        }

        public void SetRightFocus(UIElement elementToTheRight)
        {
            
        }

        public void SetTopFocus(UIElement elementAbove)
        {
            
        }
    }
}
