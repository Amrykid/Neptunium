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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel))]
    public sealed partial class AppShellView : Page
    {
        public AppShellView()
        {
            this.InitializeComponent();

            Binding pageTitleBinding = new Binding();
            pageTitleBinding.Source = NepApp.UI;
            pageTitleBinding.Path = new PropertyPath(nameof(NepApp.UI.ViewTitle));
            pageTitleBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            MobilePageTitleBlock.SetBinding(TextBlock.TextProperty, pageTitleBinding);

            Binding navItemBinding = new Binding();
            navItemBinding.Source = NepApp.UI;
            navItemBinding.Path = new PropertyPath(nameof(NepApp.UI.NavigationItems));
            navItemBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            SplitViewNavigationList.SetBinding(ItemsControl.ItemsSourceProperty, navItemBinding);
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            glassPanel.TurnOnGlass();
        }
    }
}
