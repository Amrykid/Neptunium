using System.Threading.Tasks;
using Neptunium.Data;
using System.Runtime.Serialization;

namespace Neptunium.Managers
{
    [DataContract]
    public class MusicBrainzSongMetadata
    {
        [DataMember]
        public AlbumData Album { get; set; }
        [DataMember]
        public ArtistData Artist { get; set; }
    }
}