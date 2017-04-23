using Crystal3.Model;
using Crystal3.Navigation;
using Crystal3.UI.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core
{
    public class NepAppUIManager: INotifyPropertyChanged
    {
        /// <summary>
        /// From INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region Core
        public class NepAppUINavigationItem : ModelBase
        {
            internal NepAppUINavigationItem()
            {

            }

            public string Symbol { get; internal set; }
            public string DisplayText { get; internal set; }
            public Type NavigationViewModelType { get; internal set; }
            public RelayCommand Command { get; internal set; }
            public bool IsSelected { get { return GetPropertyValue<bool>(); } internal set { SetPropertyValue<bool>(value: value); } }
        }

        private NavigationServiceBase inlineNavigationService = null;
        private string _viewTitle = "PAGE TITLE";
        private ObservableCollection<NepAppUINavigationItem> navigationItems = null;
        internal NepAppUIManager()
        {
            navigationItems = new ObservableCollection<NepAppUINavigationItem>();
            NavigationItems = new ReadOnlyObservableCollection<NepAppUINavigationItem>(navigationItems);
        }

        internal void SetNavigationService(NavigationServiceBase navService)
        {
            if (navService == null) throw new ArgumentNullException(nameof(navService));

            if (inlineNavigationService != null)
            {
                //unsubscribe from the previous nav service
                inlineNavigationService.Navigated -= InlineNavigationService_Navigated;
            }

            inlineNavigationService = navService;
            inlineNavigationService.Navigated += InlineNavigationService_Navigated;
        }

        private void InlineNavigationService_Navigated(object sender, CrystalNavigationEventArgs e)
        {
            UpdateSelectedNavigationItems();
        }

        private void UpdateSelectedNavigationItems()
        {
            foreach (NepAppUINavigationItem item in navigationItems)
                item.IsSelected = inlineNavigationService.IsNavigatedTo(item.NavigationViewModelType);
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ViewTitle { get { return _viewTitle.ToUpper(); } private set { _viewTitle = value;  RaisePropertyChanged(nameof(ViewTitle)); } }
        public ReadOnlyObservableCollection<NepAppUINavigationItem> NavigationItems { get; private set; }
        #endregion

        public void AddNavigationRoute(string displayText, Type navigationViewModel, string symbol = "")
        {
            NepAppUINavigationItem navItem = new NepAppUINavigationItem();
            navItem.DisplayText = displayText;
            navItem.Symbol = symbol;
            navItem.NavigationViewModelType = navigationViewModel;
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

            inlineNavigationService.Navigate(navItem.NavigationViewModelType, parameter);
        }
    }
}
