using System;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerNoChromeStatusChangedEventArgs : EventArgs
    {
        public bool ShouldBeInNoChromeMode { get; internal set; }
    }
}