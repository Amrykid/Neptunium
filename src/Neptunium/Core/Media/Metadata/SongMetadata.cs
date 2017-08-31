using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Model;

namespace Neptunium.Core.Media.Metadata
{
    [DataContract]
    public class SongMetadata
    {
        [DataMember]
        public string Track { get; set; }
        [DataMember]
        public string Artist { get; set; }
        [DataMember]
        public string StationPlayedOn { get; set; }
        [DataMember]
        public Uri StationLogo { get; set; }
    }

    public class ExtendedSongMetadata: SongMetadata
    {
        //todo to fill out

        public ExtendedSongMetadata()
        {

        }
        public ExtendedSongMetadata(SongMetadata original)
        {
            Track = original.Track;
            Artist = original.Artist;
            StationPlayedOn = original.StationPlayedOn;
            StationLogo = original.StationLogo;
        }

        public AlbumData Album { get; internal set; }
    }
}
