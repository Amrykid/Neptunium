using Microsoft.Toolkit.Uwp.UI.Controls;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(Neptunium.ViewModel.Server.ServerShellViewModel), Crystal3.Navigation.NavigationViewSupportedPlatform.IoT)]
    public sealed partial class ServerShellView : Page
    {
        public ServerShellView()
        {
            this.InitializeComponent();

            NothingPlayingTextBlock.SetBinding(TextBlock.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));
            NowPlayingInfo.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));

            NetworkPanel.SetBinding(Grid.DataContextProperty, new Binding() { Source = NepApp.ServerFrontEnd, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

            NepApp.SongManager.PreSongChanged += SongManager_PreSongChanged;
        }

        private void SongManager_PreSongChanged(object sender, Media.Songs.NepAppSongChangedEventArgs e)
        {
            App.Dispatcher.RunAsync(() =>
            {
                if (NepApp.SongManager.CurrentStation != null)
                {
                    NowPlayingImage.Source = new BitmapImage(NepApp.SongManager.CurrentStation.StationLogoUrlOnline);
                }

                //SongHistoryListView.DataContext = NepApp.SongManager.History.GetHistoryOfSongsAsync();
            });
        }
    }
}
