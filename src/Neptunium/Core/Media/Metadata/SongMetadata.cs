using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.Metadata
{
    public class SongMetadata
    {
        public string Track { get; set; }
        public string Artist { get; set; }
        public StationItem StationPlayedOn { get; set; }
    }

    public class ExtendedSongMetadata: SongMetadata
    {

    }
}
