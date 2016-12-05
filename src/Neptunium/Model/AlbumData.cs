using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Data
{
    [DataContract]
    public class AlbumData : ModelBase
    {
        [DataMember]
        public string Album { get; set; }
        [DataMember]
        public string AlbumCoverUrl { get; set; }
        [DataMember]
        public string AlbumID { get; set; }
        [DataMember]
        public string Artist { get; set; }
        [DataMember]
        public string ArtistID { get; set; }
        [DataMember]
        public DateTime ReleaseDate { get; set; }
    }
}
