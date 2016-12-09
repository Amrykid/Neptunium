using Neptunium.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Managers.Songs
{
    public interface ISongMetadata
    {
        AlbumData Album { get; set; }
        ArtistData Artist { get; set; }
    }
}
