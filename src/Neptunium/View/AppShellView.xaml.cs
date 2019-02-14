using Crystal3;
using Crystal3.Navigation;
using Crystal3.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Neptunium.Core.UI;
using Neptunium.ViewModel;
using Neptunium.ViewModel.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls;
using static Crystal3.UI.StatusManager.StatusManager;
using Crystal3.Messaging;
using Neptunium.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(AppShellViewModel),
        NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class AppShellView : Page, Crystal3.Messaging.IMessagingTarget
    {
        private FrameNavigationService inlineNavigationService = null;
        private ApplicationView applicationView = null;
        private CoreApplicationView coreApplicationView = null;
        public AppShellView()
        {
            this.InitializeComponent();

            coreApplicationView = CoreApplication.GetCurrentView();
            if (coreApplicationView != null)
                coreApplicationView.TitleBar.ExtendViewIntoTitleBar = true;

            applicationView = ApplicationView.GetForCurrentView();
            applicationView.TitleBar.BackgroundColor = Colors.Transparent;
            applicationView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            applicationView.TitleBar.InactiveBackgroundColor = Colors.Transparent;

            NavView.SetBinding(Microsoft.UI.Xaml.Controls.NavigationView.MenuItemsSourceProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.NavigationItems)));

            inlineNavigationService = WindowManager.GetNavigationManagerForCurrentWindow().RegisterFrameAsNavigationService(InlineFrame, FrameLevel.Two);
            UpdateSelectedNavigationItems();
            NepApp.UI.SetNavigationService(inlineNavigationService);
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;

            NepApp.UI.SetOverlayParentAndSnackBarContainer(OverlayPanel, snackBarGrid);
            App.RegisterUIDialogs();



            NavView.SetBinding(Microsoft.UI.Xaml.Controls.NavigationView.HeaderProperty, NepApp.CreateBinding(NepApp.UI, nameof(NepApp.UI.ViewTitle)));

            NowPlayingButton.SetBinding(Button.DataContextProperty, NepApp.CreateBinding(NepApp.SongManager, nameof(NepApp.SongManager.CurrentSong)));


            NepApp.UI.Overlay.OverlayedDialogShown += Overlay_DialogShown;
            NepApp.UI.Overlay.OverlayedDialogHidden += Overlay_DialogHidden;

            Messenger.AddTarget(this);
        }


        private void Overlay_DialogShown(object sender, EventArgs e)
        {
            NavView.IsBackEnabled = true;
            NavView.IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed;
            foreach (var item in NepApp.UI.NavigationItems)
                item.IsEnabled = false;
        }

        private void Overlay_DialogHidden(object sender, EventArgs e)
        {
            NavView.IsBackEnabled = inlineNavigationService.CanGoBackward;
            NavView.IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Visible;
            foreach (var item in NepApp.UI.NavigationItems)
                item.IsEnabled = true;
        }


        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            NavView.IsBackEnabled = inlineNavigationService.CanGoBackward;
            UpdateSelectedNavigationItems();
        }

        private void UpdateSelectedNavigationItems()
        {
            var viewModel = inlineNavigationService.GetNavigatedViewModel();
            if (viewModel == null) return;
            bool somethingSelected = false;
            foreach (var item in NepApp.UI.NavigationItems)
            {
                item.IsSelected = item.NavigationViewModelType == viewModel.GetType();
                if (item.IsSelected)
                {
                    NavView.SelectedItem = item;
                    somethingSelected = true;
                }
            }

            if (!somethingSelected)
            {
                NavView.SelectedItem = null;
            }
        }

        public void OnReceivedMessage(Message message, Action<object> resultCallback)
        {
            switch (message.Name)
            {
                case "ShowHandoffFlyout":
                    App.Dispatcher.RunWhenIdleAsync(() =>
                    {
                        this.GetViewModel<AppShellViewModel>()?.MediaCastingCommand.Execute(null);
                    });
                    break;
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return new string[] { "ShowHandoffFlyout" };
        }

        private void NavView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {

            }
            else
            {
                var item = args.InvokedItemContainer as NepAppUINavigationItem;
                if (item != null)
                {
                    NepApp.UI.NavigateToItem(item);
                }
            }
        }

        private void NavView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            inlineNavigationService.GoBack();
        }
    }
}
