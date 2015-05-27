using Crystal3.Navigation;
using Neptunium.MediaSourceStream;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

            this.Loaded += AppShellView_Loaded;

            BackButton.Visibility = ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons") ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void AppShellView_Loaded(object sender, RoutedEventArgs e)
        {
            //BackgroundMediaPlayer.Current.AutoPlay = true;

            //AnimeNfo - http://itori.animenfo.com:443/
            //JPopsuki - http://213.239.204.252:8000/
            var mss = new ShoutcastMediaSourceStream(new Uri("http://itori.animenfo.com:443/"));
            await mss.ConnectAsync();


            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaFailed += Current_MediaFailed;
            BackgroundMediaPlayer.Current.MediaOpened += Current_MediaOpened;
            BackgroundMediaPlayer.Current.BufferingStarted += Current_BufferingStarted;
            BackgroundMediaPlayer.Current.BufferingEnded += Current_BufferingEnded;
            BackgroundMediaPlayer.Current.SetMediaSource(mss.MediaStreamSource);
            //BackgroundMediaPlayer.Current.SetUriSource(new Uri("http://itori.animenfo.com:443/"));
            BackgroundMediaPlayer.Current.PlaybackMediaMarkerReached += Current_PlaybackMediaMarkerReached;

            await Task.Delay(5000);

            BackgroundMediaPlayer.Current.Play();
        }

        private void Current_BufferingEnded(MediaPlayer sender, object args)
        {
            
        }

        private void Current_BufferingStarted(MediaPlayer sender, object args)
        {
            
        }

        private void Current_MediaOpened(MediaPlayer sender, object args)
        {
            
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            Debug.WriteLine("BackgroundMediaPlayer.CurrentState: " + Enum.GetName(typeof(MediaPlayerState), sender.CurrentState));

            switch(sender.CurrentState)
            {
                default:
                    break;
            }
        }

        private void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            
        }

        private void Current_PlaybackMediaMarkerReached(MediaPlayer sender, PlaybackMediaMarkerReachedEventArgs args)
        {
            
        }

        private void TogglePaneButton_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
