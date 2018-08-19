using Windows.Devices.Enumeration;
using Windows.Phone.Media.Devices;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neptunium.Core.Media.Audio
{
    internal class MobileHeadsetDetector : BaseHeadsetDetector
    {
        private DeviceWatcher watcher;
        private string[] headPhoneDeviceIds = new string[]
        {
            @"\\?\SWD#MMDEVAPI#{0.0.0.00000000}.{10a8b185-48a8-413b-b914-bfba7bde5e4e}#{e6327cad-dcec-4949-ae8a-991e976a79d2}", //Lumia 830
            @"\\?\SWD#MMDEVAPI#{0.0.0.00000000}.{af8f0b3f-d3c7-4344-ad0c-d75d24429314}#{e6327cad-dcec-4949-ae8a-991e976a79d2}", //Lumia 950

        };

        public MobileHeadsetDetector()
        {
            //based on code from: https://stackoverflow.com/questions/40256940/how-to-get-headphones-plug-event-in-uwp/40281239#40281239

            watcher = DeviceInformation.CreateWatcher(DeviceClass.AudioRender);
            watcher.Added += Watcher_Added;
            watcher.Removed += Watcher_Removed;
            watcher.Updated += Watcher_Updated;
            watcher.Start();

            //AudioRoutingManager.GetDefault().AudioEndpointChanged += MobileHeadsetDetector_AudioEndpointChanged;
        }

        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {

        }

        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (args.Kind == DeviceInformationKind.DeviceInterface && IsHeadphones(args.Id, args.Properties))
            {
                SetHeadsetStatus(false);
            }
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (args.Kind == DeviceInformationKind.DeviceInterface && IsHeadphones(args.Id, args.Properties))
            {
                SetHeadsetStatus(true);
            }
        }

        private bool IsHeadphones(string id, IReadOnlyDictionary<string,object> properties)
        {
            return properties["System.Devices.Icon"].Equals(@"%windir%\system32\mmres.dll,-3015"); //headphones always have this icon on Windows 10 Mobile.
        }
    }
}