using Crystal3;
using Crystal3.Navigation;
using Microsoft.Graphics.Canvas.Effects;
using Neptunium.Data.Stations;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(StationInfoViewModel))]
    public sealed partial class StationInfoView : Page
    {
        private StationInfoViewModel viewModel = null;
        private Color blurColor = Color.FromArgb(255, 245, 245, 245);

        public StationInfoView()
        {
            this.InitializeComponent();
            blurColor = Color.FromArgb(255, 245, 245, 245);

            if (CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                bannerBar.Visibility = Visibility.Visible;
                bannerBar.BannerText = "Please the Menu button on your controller to play this station.";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SongHistoryPanel.MessageReceived += SongHistoryPanel_MessageReceived;
        }

        private void SongHistoryPanel_MessageReceived(object sender, Crystal3.UI.FragmentContentViewer.FragmentContentViewerUIMessageReceivedEventArgs e)
        {
            switch (e.Message.ToLower())
            {
                case "show":
                    SongHistoryPanel.Visibility = Visibility.Visible;
                    break;
                case "hide":
                    SongHistoryPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            viewModel = (args.NewValue as StationInfoViewModel);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            SongHistoryPanel.MessageReceived -= SongHistoryPanel_MessageReceived;
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Station" && Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile() != null)
            {
                viewModel = (this.DataContext as StationInfoViewModel);

                if (viewModel.Station == null) return;

                var imgSrc = viewModel.Station.Logo;

                if (string.IsNullOrWhiteSpace(imgSrc))
                {
                    //turn off the glass

                    GlassPanel.TurnOffGlass();
                }
                else
                {
                    


                    //setup to use glass with a blur of the dominant color from the station logo
                    try
                    {
                        blurColor = await StationSupplementaryDataManager.GetStationLogoDominantColorAsync(viewModel.Station);
                    }
                    catch (Exception)
                    {
                        //set the default blur color.
                        blurColor = Color.FromArgb(255, 245, 245, 245);
                    }

                    //turn on glass

                    GlassPanel.ChangeBlurColor(blurColor);

                    BackDropGridImageBrush.ImageSource = new BitmapImage(new Uri(imgSrc));
                }
            }
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.GamepadMenu:
                    if (playButton.Command.CanExecute(playButton.CommandParameter))
                        playButton.Command.Execute(playButton.CommandParameter);
                    break;
            }
        }
    }
}
