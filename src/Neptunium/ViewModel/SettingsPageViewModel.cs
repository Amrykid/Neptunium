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
            base.OnNavigatedTo(sender, e);
        }

        public bool ShowSongNotification
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.ShowSongNotifications); }
            set { NepApp.Settings.SetSetting(AppSettings.ShowSongNotifications, value); }
        }

        public bool FindSongMetadata
        {
            get { return (bool)NepApp.Settings.GetSetting(AppSettings.TryToFindSongMetadata); }
            set { NepApp.Settings.SetSetting(AppSettings.TryToFindSongMetadata, value); }
        }
    }
}
