using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers
{
    public class CarModeManagerCarModeStatusChangedEventArgs: EventArgs
    {
        internal CarModeManagerCarModeStatusChangedEventArgs(bool isInCarMode)
        {
            IsInCarMode = isInCarMode;
        }

        public bool IsInCarMode { get; private set; }
    }
}
