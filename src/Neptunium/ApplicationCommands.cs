using Crystal3.InversionOfControl;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Media;
using Neptunium.Services.Vibration;
using Neptunium.ViewModel;
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
                    HapticFeedbackService.TapVibration();

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

            GoToStationCommand = new RelayCommand(station =>
            {
                HapticFeedbackService.TapVibration();

                WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(FrameLevel.Two)
                .NavigateTo<StationInfoViewModel>(((StationModel)station).Name);
            }, station => station != null);
        }

        public RelayCommand GoToStationCommand { get; private set; }
        public RelayCommand PlayStationCommand { get; private set; }
    }
}
