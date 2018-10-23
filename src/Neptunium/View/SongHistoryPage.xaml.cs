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
    [Crystal3.Navigation.NavigationViewModel(typeof(SongHistoryPageViewModel), NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class SongHistoryPage : Page
    {
        public SongHistoryPage()
        {
            this.InitializeComponent();
        }
    }
}
