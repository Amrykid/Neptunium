using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel.Server
{
    public class ServerShellViewModel: ViewModelBase
    {
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            //if its IoT
            NepApp.ServerFrontEnd.DataReceived += ServerFrontEnd_DataReceived;

            base.OnNavigatedTo(sender, e);
        }

        private void ServerFrontEnd_DataReceived(object sender, NepAppServerFrontEndManager.NepAppServerFrontEndManagerDataReceivedEventArgs e)
        {
           
        }
    }
}
