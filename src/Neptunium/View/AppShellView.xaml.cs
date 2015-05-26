using Crystal3.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
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
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel))]
    public sealed partial class AppShellView : Page
    {
        public AppShellView()
        {
            this.InitializeComponent();


            //AnimeNfo
            BackgroundMediaPlayer.Current.SetMediaSource()
            BackgroundMediaPlayer.Current.SetUriSource(new Uri("http://itori.animenfo.com:443/"));
            BackgroundMediaPlayer.Current.PlaybackMediaMarkerReached += Current_PlaybackMediaMarkerReached;
            BackgroundMediaPlayer.Current.Play();
            

            BackButton.Visibility = ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons") ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Current_PlaybackMediaMarkerReached(MediaPlayer sender, PlaybackMediaMarkerReachedEventArgs args)
        {
            
        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
