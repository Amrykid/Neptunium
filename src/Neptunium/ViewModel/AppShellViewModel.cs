using Crystal3.Model;
using Crystal3.UI.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel
{
    public class AppShellViewModel: ViewModelBase
    {
        public AppShellViewModel()
        {
            GoToStationsViewCommand = new CRelayCommand(x =>
            {
                Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                .NavigateTo<StationsViewViewModel>();
            });

            GoBackCommand = new CRelayCommand(x =>
            {
                Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two)
                .GoBack();
            },
            x =>
            {
                return Crystal3.Navigation.WindowManager.GetNavigationManagerForCurrentWindow()
                .GetNavigationServiceFromFrameLevel(Crystal3.Navigation.FrameLevel.Two).CanGoBackward;
            });
        }

        public CRelayCommand GoToStationsViewCommand { get; private set; }

        public CRelayCommand GoBackCommand { get; private set; }
    }
}
