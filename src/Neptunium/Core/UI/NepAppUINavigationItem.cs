using Crystal3.Model;
using Crystal3.UI.Commands;
using System;
using System.ComponentModel;
using static Neptunium.NepApp;

namespace Neptunium.Core.UI
{
    public class NepAppUINavigationItem : ModelBase
    {
        internal NepAppUINavigationItem()
        {

        }

        public string Symbol { get; internal set; }
        public string DisplayText { get; internal set; }
        public Type NavigationViewModelType { get; internal set; }
        public RelayCommand Command { get; internal set; }
        public bool IsSelected { get { return GetPropertyValue<bool>(); } set { SetPropertyValue<bool>(value: value); } }
    }
}
