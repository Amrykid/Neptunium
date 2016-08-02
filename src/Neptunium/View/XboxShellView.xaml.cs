using Crystal3.Model;
using Crystal3.Navigation;
using Neptunium.Logging;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxShellView : Page
    {
        private NavigationService inlineNavService = null;
        public XboxShellView()
        {
            this.InitializeComponent();

            inlineNavService = WindowManager.GetNavigationManagerForCurrentWindow()
              .RegisterFrameAsNavigationService(xboxMainFrame, FrameLevel.Two);
            inlineNavService.NavigationServicePreNavigatedSignaled += AppShellView_NavigationServicePreNavigatedSignaled;
        }

        private void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ((Pivot)sender).SelectedIndex;

            switch(index)
            {
                case 0:
                    if (!inlineNavService.IsNavigatedTo<StationsViewViewModel>())
                        inlineNavService.NavigateTo<StationsViewViewModel>();
                    break;
                case 1:
                    if (!inlineNavService.IsNavigatedTo<SongHistoryViewModel>())
                        inlineNavService.NavigateTo<SongHistoryViewModel>();
                    break;
                case 2:
                    if (!inlineNavService.IsNavigatedTo<SettingsViewViewModel>())
                        inlineNavService.NavigateTo<SettingsViewViewModel>();
                    break;
            }
        }

        private void AppShellView_NavigationServicePreNavigatedSignaled(object sender, NavigationServicePreNavigatedSignaledEventArgs e)
        {
            LogManager.Info(typeof(XboxShellView), "XboxShellView_NavigationServicePreNavigatedSignaled");

            RefreshNavigationSplitViewState(e.ViewModel);
        }

        private void RefreshNavigationSplitViewState(ViewModelBase viewModelToGoTo)
        {
           App.Dispatcher.RunAsync(() =>
            {
                
            });
        }
    }
}
