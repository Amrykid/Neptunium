using Windows.Gaming.Input;
using System.Linq;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Core;

namespace Neptunium.Core.Media.Audio
{
    internal class XboxHeadsetDetector : BaseHeadsetDetector
    {
        private RawGameController usersConnectedGamePad = null;
        public XboxHeadsetDetector()
        {
            usersConnectedGamePad = RawGameController.RawGameControllers.FirstOrDefault(x => x.User == Crystal3.CrystalApplication.GetCurrentAsCrystalApplication().CurrentUser);
            if (usersConnectedGamePad != null)
            {
                usersConnectedGamePad.HeadsetConnected += UsersConnectedGamePad_HeadsetConnected;
                usersConnectedGamePad.HeadsetDisconnected += UsersConnectedGamePad_HeadsetDisconnected;
                SetHeadsetStatus(usersConnectedGamePad.Headset != null);
            }
        }

        private void UsersConnectedGamePad_HeadsetDisconnected(IGameController sender, Headset args)
        {
            SetHeadsetStatus(false);
        }

        private void UsersConnectedGamePad_HeadsetConnected(IGameController sender, Headset args)
        {
            SetHeadsetStatus(true);
        }
    }
}