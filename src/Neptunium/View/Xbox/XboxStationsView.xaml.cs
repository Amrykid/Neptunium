using Crystal3.Navigation;
using Crystal3.UI;
using Neptunium.Media;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View.Xbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(StationsViewViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxStationsView : Page
    {

        private ToggleButton shellPageMenuToggleBtn = null;
        public XboxStationsView()
        {
            this.InitializeComponent();

            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.Control", "XYFocusUp"))
            {
                //var topFrame = WindowManager.GetNavigationManagerForCurrentWindow().RootNavigationService.NavigationFrame;
                //var shellPage = topFrame.Content as AppShellView;
                //if (shellPage != null)
                //{
                //    shellPageMenuToggleBtn = shellPage.FindName("MobileTogglePaneButton") as ToggleButton;
                //    //StationsListBox.XYFocusUp = toggleBtn;
                //}
            }
        }

        private void StationsListBox_ItemClick(object sender, ItemClickEventArgs e)
        {
            var appCommands = (App.Current.Resources["AppCommands"] as ApplicationCommands);
            var item = e.ClickedItem;

            if (appCommands.GoToStationCommand.CanExecute(item))
                appCommands.GoToStationCommand.Execute(item);
        }

        private void StationsListBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StationsListBox.Focus(FocusState.Programmatic);

            if (StationMediaPlayer.IsPlaying && StationMediaPlayer.CurrentStation != null)
            {
                StationsListBox.SelectedItem = StationMediaPlayer.CurrentStation;
            }
            else
            {
                var viewModel = this.GetViewModel<StationsViewViewModel>();

                if (viewModel != null)
                {
                    if (viewModel.Stations == null)
                    {
                        //waits for the stations to load.
                        await viewModel.WaitForPropertyChangeAsync<object>("Stations");
                    }
                    StationsListBox.SelectedIndex = 0;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StationMediaPlayer.ConnectingStatusChanged += StationMediaPlayer_ConnectingStatusChanged;

            base.OnNavigatedTo(e);
        }

        private void StationMediaPlayer_ConnectingStatusChanged(object sender, StationMediaPlayerConnectingStatusChangedEventArgs e)
        {
            App.Dispatcher.RunWhenIdleAsync(() =>
            {
                this.IsEnabled = !e.IsConnecting;
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StationMediaPlayer.ConnectingStatusChanged -= StationMediaPlayer_ConnectingStatusChanged;

            base.OnNavigatedFrom(e);
        }
    }
}
