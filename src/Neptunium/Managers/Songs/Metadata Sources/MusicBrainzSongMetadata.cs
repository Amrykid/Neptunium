using System.Threading.Tasks;
using Neptunium.Data;
using System.Runtime.Serialization;

namespace Neptunium.Managers.Songs
{
    [DataContract]
    public class MusicBrainzSongMetadata: ISongMetadata
    {
        [DataMember]
        public AlbumData Album { get; set; }
        [DataMember]
        public ArtistData Artist { get; set; }
    }
}