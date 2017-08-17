using Crystal3.UI;
using Crystal3.Utilities;
using Neptunium.Glue;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

            stationsGridView.SingleSelectionFollowsFocus = true;

            long itemsSourceHandler = 0;
            itemsSourceHandler = stationsGridView.RegisterPropertyChangedCallback(GridView.ItemsSourceProperty, new DependencyPropertyChangedCallback(async (obj, dp) =>
            {
                if (obj.GetValue(dp) != null)
                {
                    //try and force focus on the first item when we load this page.
                    stationsGridView.UnregisterPropertyChangedCallback(GridView.ItemsSourceProperty, itemsSourceHandler);
                    await Task.Delay(1000);
                    var firstItem = (GridViewItem)stationsGridView.ContainerFromIndex(0);
                    firstItem.Focus(FocusState.Keyboard);
                }
            }));
        }


        private GridViewItem focusedItem = null;
        public void PreserveFocus()
        {
            //requires SelectionMode = Single
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

        private void stationsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem;
            var viewModel = this.GetViewModel<StationsPageViewModel>();
            if (viewModel.ShowStationInfoCommand.CanExecute(item))
                viewModel.ShowStationInfoCommand.Execute(item);
        }
    }
}
