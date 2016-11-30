using Crystal3.Navigation;
using Neptunium.Media;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public XboxStationsView()
        {
            this.InitializeComponent();
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
            StationsListBox.Focus(FocusState.Pointer);

            if (args.NewValue != null)
            {
                try
                {
                    StationsListBox.SelectedIndex = 0;
                }
                catch (Exception) { }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StationsListBox.Focus(FocusState.Pointer);
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
