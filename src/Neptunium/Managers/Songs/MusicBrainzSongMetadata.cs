using System.Threading.Tasks;
using Neptunium.Data;

namespace Neptunium.Managers
{
    public class MusicBrainzSongMetadata
    {
        public AlbumData Album { get; internal set; }
        public ArtistData Artist { get; internal set; }
    }
}