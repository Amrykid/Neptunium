using Crystal3.UI.Commands;
using Neptunium.Data;
using Neptunium.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public class ApplicationCommands
    {
        public ApplicationCommands()
        {
            PlayStationCommand = new CRelayCommand(async station =>
                {
                    var result = await ShoutcastStationMediaPlayer.PlayStationAsync((StationModel)station);


                }, station => station != null);
        }

        public CRelayCommand PlayStationCommand { get; private set; }
    }
}
