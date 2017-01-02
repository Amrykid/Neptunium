using Crystal3.Navigation;
using Crystal3.UI;
using Crystal3.Utilities;
using Microsoft.Graphics.Canvas.Effects;
using Neptunium.Media;
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
        public NowPlayingView()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateIsMobileView(Window.Current.Bounds.Width < 720);

            StationMediaPlayer.IsPlayingChanged += StationMediaPlayer_IsPlayingChanged;

            if (StationMediaPlayer.IsPlaying && StationMediaPlayer.CurrentStation != null)
            {
                //var accentColor = (Color)this.Resources["SystemAccentColor"];

                GlassPanel.ChangeBlurColor(await Neptunium.Data.Stations.StationSupplementaryDataManager.GetStationLogoDominantColorAsync(StationMediaPlayer.CurrentStation));
            }
        }

        private void StationMediaPlayer_IsPlayingChanged(object sender, EventArgs e)
        {
        }

        private void UpdateIsMobileView(bool isMobileView)
        {
            IsMobileView = isMobileView;
            IsMobileViewChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            StationMediaPlayer.IsPlayingChanged -= StationMediaPlayer_IsPlayingChanged;
        }

        public bool IsMobileView { get; private set; }
        public event EventHandler IsMobileViewChanged;

        private void VisualStateGroup_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateIsMobileView(e.NewState == PhoneVisualState);
        }

        private void VisualStateGroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {

        }
    }
}
