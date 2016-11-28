using Crystal3.Navigation;
using Crystal3.UI;
using Crystal3.Utilities;
using Microsoft.Graphics.Canvas.Effects;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(NowPlayingViewViewModel), NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class NowPlayingView : Page
    {
        private NowPlayingViewViewModel viewModel = null;

        public NowPlayingView()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GlassPanel.TurnOffGlass(); //turn off the glass
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NowPlayingBackgroundImage")
            {
                var viewModel = (this.DataContext as NowPlayingViewViewModel);

                await Task.Delay(500);

                var imgSrc = viewModel.NowPlayingBackgroundImage;

                if (string.IsNullOrWhiteSpace(imgSrc))
                {
                    GlassPanel.TurnOffGlass(); //turn off the glass
                }
                else
                {
                    //todo check if url doesn't 404

                    GlassPanel.TurnOnGlass(); //turn on glass
                }
            }
        }

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            viewModel = (args.NewValue as NowPlayingViewViewModel);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }
}
