using Crystal3.UI.Commands;
using System;

namespace Neptunium.Core.UI
{
    public class NepAppUINavigationItem : Microsoft.UI.Xaml.Controls.NavigationViewItem
    {
        internal NepAppUINavigationItem()
        {

        }

        public string Symbol { get; internal set; }
        public string DisplayText
        {
            //needed from the switch from ModelBase to NavigationViewItem
            get { return base.Content as string; }
            set { base.Content = value; }
        }
        public Type NavigationViewModelType { get; internal set; }
        public RelayCommand Command { get; internal set; }
        public string PageHeaderText { get; internal set; }
    }
}
