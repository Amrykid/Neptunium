using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;
using Neptunium.Core;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel: ViewModelBase
    {
        public AppShellViewModel()
        {
            App.Current.UnhandledException += Current_UnhandledException;

            NepApp.UI.AddNavigationRoute("Stations", typeof(StationsPageViewModel), "");
        }

        private void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception is NeptuniumException)
            {
                e.Handled = true;
            }
        }

        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            base.OnNavigatedTo(sender, e);

            var stationsPage = NepApp.UI.NavigationItems.FirstOrDefault(X => X.NavigationViewModelType == typeof(StationsPageViewModel));
            if (stationsPage == null) throw new Exception("Stations page not found.");
            NepApp.UI.NavigateToItem(stationsPage);
        }
    }
}
