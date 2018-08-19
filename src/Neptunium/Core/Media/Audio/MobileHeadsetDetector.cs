using Windows.Devices.Enumeration;

namespace Neptunium.Core.Media.Audio
{
    internal class MobileHeadsetDetector : BaseHeadsetDetector
    {
        private DeviceWatcher watcher;

        public MobileHeadsetDetector()
        {
            //based on code from: https://stackoverflow.com/questions/40256940/how-to-get-headphones-plug-event-in-uwp/40281239#40281239

            watcher = DeviceInformation.CreateWatcher(DeviceClass.AudioRender);
            watcher.Added += Watcher_Added;
            watcher.Removed += Watcher_Removed;
            watcher.Updated += Watcher_Updated;
        }

        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            
        }

        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            SetHeadsetStatus(false);
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            SetHeadsetStatus(true);
        }
    }
}