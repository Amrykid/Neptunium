using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Audio
{
    public interface IHeadsetDetector
    {
        bool IsHeadsetPluggedIn { get; }
        event EventHandler IsHeadsetPluggedInChanged;
    }

    public abstract class BaseHeadsetDetector : IHeadsetDetector
    {
        public bool IsHeadsetPluggedIn { get; private set; }

        public event EventHandler IsHeadsetPluggedInChanged;

        protected void SetHeadsetStatus(bool pluggedIn)
        {
            bool old = IsHeadsetPluggedIn;
            IsHeadsetPluggedIn = pluggedIn;
            if (old != pluggedIn)
            {
                IsHeadsetPluggedInChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class MockHeadsetDetector : BaseHeadsetDetector
    {
    }
}
