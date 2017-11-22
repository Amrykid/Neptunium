using System;

namespace Neptunium.Core.Settings
{
    public class NepAppSettingChangedEventArgs : EventArgs
    {
        public AppSettings ChangedSetting { get; internal set; }
        public object NewValue { get; internal set; }
    }
}