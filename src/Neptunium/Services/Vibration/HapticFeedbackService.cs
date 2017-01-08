using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.Devices.Notification;
using Windows.Storage;

namespace Neptunium.Services.Vibration
{
    public static class HapticFeedbackService
    {
        static VibrationDevice vibrationDevice = null;
        static HapticFeedbackService()
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
                vibrationDevice = VibrationDevice.GetDefault();
        }

        public static void TapVibration()
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                if ((bool)ApplicationData.Current.LocalSettings.Values[AppSettings.UseHapticFeedbackForNavigation])
                {
                    vibrationDevice?.Vibrate(TimeSpan.FromMilliseconds(32));
                }
            }
        }
    }
}
