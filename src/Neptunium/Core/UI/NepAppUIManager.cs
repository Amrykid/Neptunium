using Crystal3.Navigation;
using Crystal3.UI.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using static Neptunium.NepApp;

namespace Neptunium.Core.UI
{
    public class NepAppUIManager : INotifyPropertyChanged, INepAppFunctionManager
    {
        /// <summary>
        /// From INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region Core
        private NavigationServiceBase inlineNavigationService = null;
        private string _viewTitle = "PAGE TITLE";
        private ObservableCollection<NepAppUINavigationItem> navigationItems = null;
        private WindowService windowService = null;


        public string ViewTitle { get { return _viewTitle.ToUpper(); } private set { _viewTitle = value; RaisePropertyChanged(nameof(ViewTitle)); } }
        public ReadOnlyObservableCollection<NepAppUINavigationItem> NavigationItems { get; private set; }
        public NepAppUIManagerNotifier Notifier { get; private set; }
        public NepAppUIManagerDialogCoordinator Overlay { get; private set; }
        public NepAppUILockScreenManager LockScreen { get; private set; }
        public NepAppUILiveTileHandler LiveTileHandler { get; private set; }
        public NepAppUIToastNotificationHandler ToastHandler { get; private set; }

        internal NepAppUIManager()
        {
            navigationItems = new ObservableCollection<NepAppUINavigationItem>();
            NavigationItems = new ReadOnlyObservableCollection<NepAppUINavigationItem>(navigationItems);
            Notifier = new NepAppUIManagerNotifier();
            LockScreen = new NepAppUILockScreenManager();
            LiveTileHandler = new NepAppUILiveTileHandler(this);
            ToastHandler = new NepAppUIToastNotificationHandler();
            windowService = WindowManager.GetWindowServiceForCurrentWindow();
        }

        internal void SetNavigationService(NavigationServiceBase navService)
        {
            if (navService == null) throw new ArgumentNullException(nameof(navService));

            if (inlineNavigationService != null)
            {
                //unsubscribe from the previous nav service
                ((FrameNavigationService)inlineNavigationService).Navigated -= NepAppUIManager_Navigated;
            }

            inlineNavigationService = navService;
            ((FrameNavigationService)inlineNavigationService).Navigated += NepAppUIManager_Navigated;
        }

        private void NepAppUIManager_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            UpdateSelectedNavigationItems();

            windowService.SetAppViewBackButtonVisibility(inlineNavigationService.CanGoBackward);
        }

        internal void SetOverlayParentAndSnackBarContainer(Grid parentControl, Grid snackBarContainer)
        {
            if (parentControl == null) throw new ArgumentNullException(nameof(parentControl));
            if (snackBarContainer == null) throw new ArgumentNullException(nameof(snackBarContainer));

            Overlay = new NepAppUIManagerDialogCoordinator(this, parentControl, snackBarContainer);
        }

        internal void UpdateSelectedNavigationItems()
        {
            var pageType = ((FrameNavigationService)inlineNavigationService).NavigationFrame.CurrentSourcePageType;

            foreach (NepAppUINavigationItem navItem in navigationItems)
            {
                navItem.IsSelected = false;
            }

            var navigationManager = WindowManager.GetNavigationManagerForCurrentWindow();

            NepAppUINavigationItem item = null;
            item = navigationItems.FirstOrDefault(x =>
            {
                var navInfo = navigationManager.GetViewModelInfo(x.NavigationViewModelType);
                return pageType == navInfo.ViewType;
            });

            if (item != null)
            {
                item.IsSelected = true;
                ViewTitle = item.DisplayText;
            }
            else
            {
                ViewTitle = "";
            }
        }

        internal void ClearNavigationRoutes()
        {
            navigationItems.Clear();
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task<IUICommand> ShowInfoDialogAsync(string title, string message, IEnumerable<IUICommand> commands = null)
        {
            if (App.Dispatcher.HasThreadAccess)
            {
                MessageDialog dialog = new MessageDialog(message);
                dialog.Title = title;
                if (commands != null)
                {
                    foreach (IUICommand command in commands)
                    {
                        dialog.Commands.Add(command);
                    }
                }
                return await dialog.ShowAsync();
            }
            else
            {
                return await await App.Dispatcher.RunWhenIdleAsync(() =>
                {
                    MessageDialog dialog = new MessageDialog(message);
                    dialog.Title = title;
                    if (commands != null)
                    {
                        foreach (IUICommand command in commands)
                        {
                            dialog.Commands.Add(command);
                        }
                    }
                    return dialog.ShowAsync();
                });
            }
        }
        public async Task<bool> ShowYesNoDialogAsync(string title, string message)
        {
            var yes = new UICommand("Yes");
            var no = new UICommand("No");
            var result = await ShowInfoDialogAsync(title, message, new IUICommand[] { yes, no });

            return result == yes;
        }
        #endregion

        public void AddNavigationRoute(string displayText, Type navigationViewModel, string symbol = "", string pageHeader = null)
        {
            if (navigationItems.Any(x => x.DisplayText.Equals(displayText))) return;

            NepAppUINavigationItem navItem = new NepAppUINavigationItem();
            navItem.DisplayText = displayText;
            navItem.Symbol = symbol;
            navItem.NavigationViewModelType = navigationViewModel;
            navItem.PageHeaderText = pageHeader ?? displayText;
            navItem.Command = new RelayCommand(x =>
            {
                NavigateToItem(navItem, x);
            });
            navigationItems.Add(navItem);
        }

        public void NavigateToItem(NepAppUINavigationItem navItem, object parameter = null)
        {
            if (inlineNavigationService == null)
                throw new InvalidOperationException(nameof(inlineNavigationService) + " is null.");

            if (!((FrameNavigationService)inlineNavigationService).IsNavigatedTo(navItem.NavigationViewModelType))
                ((FrameNavigationService)inlineNavigationService).Navigate(navItem.NavigationViewModelType, parameter);

            //UpdateSelectedNavigationItems();

            ViewTitle = navItem.PageHeaderText;
        }

        internal void ClearLiveTileAndMediaNotifcation()
        {
            if (App.GetDevicePlatform() == Crystal3.Core.Platform.Desktop || App.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                //clears the tile if we're suspending.
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }

            if (!NepApp.MediaPlayer.IsPlaying)
            {
                //removes the now playing notification from the action center.
                ToastNotificationManager.History.Remove(NepAppUIManagerNotifier.SongNotificationTag);
            }
        }


        #region No-Chrome Mode
        public event EventHandler<NepAppUIManagerNoChromeStatusChangedEventArgs> NoChromeStatusChanged;

        public bool IsInNoChromeMode { get; private set; }

        public void ActivateNoChromeMode()
        {
            if (IsInNoChromeMode) return;

            IsInNoChromeMode = true;

            NoChromeStatusChanged?.Invoke(this, new NepAppUIManagerNoChromeStatusChangedEventArgs()
            {
                ShouldBeInNoChromeMode = IsInNoChromeMode
            });
        }

        public void DeactivateNoChromeMode()
        {
            if (!IsInNoChromeMode) return;

            IsInNoChromeMode = false;

            NoChromeStatusChanged?.Invoke(this, new NepAppUIManagerNoChromeStatusChangedEventArgs()
            {
                ShouldBeInNoChromeMode = IsInNoChromeMode
            });
        }
        #endregion
    }
}
