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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.AppShellViewModel), Crystal3.Navigation.NavigationViewSupportedPlatform.IoT)]
    public sealed partial class IoTShellView : Page
    {
        public IoTShellView()
        {
            this.InitializeComponent();

            NothingPlayingTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NowPlayingPanel.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));

            NetworkPanel.SetBinding(Grid.DataContextProperty, new Binding() { Source = NepApp.ServerFrontEnd, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });


            //auto play a station after 5 seconds.
            System.Threading.Tasks.Task.Delay(5000).ContinueWith(async x =>
            {
                var station = await NepApp.Stations.GetStationByNameAsync("AnimeNfo");
                await NepApp.MediaPlayer.TryStreamStationAsync(station.Streams[0]);
            });
        }
    }
}
