using Crystal3.InversionOfControl;
using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public class ApplicationCommands
    {
        public ApplicationCommands()
        {
            PlayStationCommand = new RelayCommand(async station =>
                {
                    if (Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile() != null)
                    {
                        var result = await StationMediaPlayer.PlayStationAsync((StationModel)station);
                    }
                    else
                    {
                        var dialogService = IoC.Current.Resolve<Crystal3.Core.IMessageDialogService>();

                        if (dialogService != null)
                        {
                            await dialogService.ShowAsync(
                                string.Format("We are unable to connect to {0}. You do not have a suitable internet connection.", ((StationModel)station).Name), "No Internet Connection");
                        }
                    }

                }, station => station != null);
        }

        public RelayCommand PlayStationCommand { get; private set; }
    }
}
