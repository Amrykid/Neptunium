using Crystal3.Navigation;
using Crystal3.UI.Converters;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                long itemsSourceHandler = 0;
                itemsSourceHandler = SongHistoryListView.RegisterPropertyChangedCallback(GridView.ItemsSourceProperty, new DependencyPropertyChangedCallback(async (obj, dp) =>
                {
                    IEnumerable<object> collection = obj.GetValue(dp) as IEnumerable<object>;
                    if (collection != null)
                    {
                        //try and force focus on the first item when we load this page.
                        SongHistoryListView.UnregisterPropertyChangedCallback(GridView.ItemsSourceProperty, itemsSourceHandler);

                        if (collection.Count() == 0) return;

                        //wait until we can get an item from the station grid view to focus. this is a hack but it'll have to do.
                        ListViewItem firstItem = null;
                        do
                        {
                            firstItem = (ListViewItem)SongHistoryListView.ContainerFromIndex(0);
                            await Task.Delay(50);
                        } while (firstItem == null);

                        firstItem.Focus(FocusState.Keyboard);

                    }
                }));
            }
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
