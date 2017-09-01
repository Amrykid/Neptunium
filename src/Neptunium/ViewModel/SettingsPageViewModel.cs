using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crystal3.Navigation;

namespace Neptunium.ViewModel
{
    public class SettingsPageViewModel: ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            NepApp.Settings.GetAllSettings();

            base.OnNavigatedTo(sender, e);
        }
    }
}
