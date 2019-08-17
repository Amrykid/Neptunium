using Crystal3.Navigation;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(SongHistoryPageViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxSongHistoryPage : Page, Neptunium.Glue.IXboxInputPage
    {
        public XboxSongHistoryPage()
        {
            this.InitializeComponent();


            long itemsSourceHandler = 0;
            itemsSourceHandler = SongHistoryListView.RegisterPropertyChangedCallback(GridView.ItemsSourceProperty, new DependencyPropertyChangedCallback(async (obj, dp) =>
            {
                IEnumerable<object> collection = obj.GetValue(dp) as IEnumerable<object>;
                if (collection != null)
                {
                    //try and force focus on the first item when we load this page.
                    SongHistoryListView.UnregisterPropertyChangedCallback(GridView.ItemsSourceProperty, itemsSourceHandler);

                    if (collection.Count() == 0) return;

                    //wait until we can get an item from the song history list view to focus. this is a hack but it'll have to do.
                    ListViewItem firstItem = null;

                    do
                    {
                        firstItem = (ListViewItem)SongHistoryListView.ContainerFromIndex(0);
                        await Task.Delay(50);
                    } while (firstItem == null);

                    firstItem.Focus(FocusState.Keyboard);

                    SongHistoryListView.SelectedIndex = 0;
                }
            }));
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
