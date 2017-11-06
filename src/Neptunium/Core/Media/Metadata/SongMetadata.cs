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

        [IgnoreDataMember]
        public bool IsUnknownMetadata { get; internal set; } = false;
        [IgnoreDataMember]
        public string RadioProgram { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Artist ?? "Unknown Artist", Track ?? "Unknown Track");
        }
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
            IsUnknownMetadata = original.IsUnknownMetadata;
            RadioProgram = original.RadioProgram;
        }

        public AlbumData Album { get; internal set; }
        public ArtistData ArtistInfo { get; internal set; }
        public JPopAsiaArtistData JPopAsiaArtistInfo { get; internal set; }
        public Uri FanArtTVBackgroundUrl { get; internal set; }
    }
}
