using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Data
{
    public class AlbumData : ModelBase
    {
        public string Album { get; internal set; }
        public string AlbumCoverUrl { get; internal set; }
        public string AlbumID { get; internal set; }
        public string Artist { get; internal set; }
        public string ArtistID { get; internal set; }
        public DateTime ReleaseDate { get; internal set; }
    }
}
