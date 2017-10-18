using Neptunium.Core.Media.Metadata;
using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Neptunium.NepApp;

namespace Neptunium.Core.Media.Songs
{
    public class NepAppSongManager: INepAppFunctionManager
    {
        internal NepAppSongManager()
        {

        }

        internal async void HandleMetadata(SongMetadata songMetadata, StationStream currentStrean)
        {
            
        }
    }
}
