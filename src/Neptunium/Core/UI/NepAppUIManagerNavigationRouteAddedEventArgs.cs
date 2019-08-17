using System;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerNavigationRouteAddedEventArgs: EventArgs
    {
        public NepAppUINavigationItem NavigationItem { get; private set; }

        internal NepAppUIManagerNavigationRouteAddedEventArgs(NepAppUINavigationItem navItem)
        {
            NavigationItem = navItem;
        }
    }
}