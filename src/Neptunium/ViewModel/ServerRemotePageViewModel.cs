using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel
{
    public class ServerRemotePageViewModel : ViewModelBase
    {
        private Neptunium.Core.NepAppServerFrontEndManager.NepAppServerClient serverClient = null;
        protected override void OnNavigatedTo(object sender, CrystalNavigationEventArgs e)
        {
            Neptunium.Core.NepAppServerFrontEndManager.NepAppServerClient serverClient = new Core.NepAppServerFrontEndManager.NepAppServerClient();

            ConnectCommand = new ManualRelayCommand(async parameter =>
            {
                try
                {
                    await serverClient.TryConnectAsync(IPAddress.Parse((string)parameter);
                    IsConnected = true;

                    serverClient.
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                }
            });

            DisconnectCommand = new ManualRelayCommand(parameter =>
            {

            });

            ConnectCommand.SetCanExecute(!IsConnected);
            DisconnectCommand.SetCanExecute(IsConnected);

            base.OnNavigatedTo(sender, e);
        }

        protected override void OnNavigatedFrom(object sender, CrystalNavigationEventArgs e)
        {
            base.OnNavigatedFrom(sender, e);
        }

        public bool IsConnected
        {
            get { return GetPropertyValue<bool>(); }
            private set
            {
                SetPropertyValue<bool>(value: value);

                ConnectCommand.SetCanExecute(!value);
                DisconnectCommand.SetCanExecute(value);
            }
        }

        public ManualRelayCommand ConnectCommand { get; private set; }

        public ManualRelayCommand DisconnectCommand { get; private set; }
    }
}
