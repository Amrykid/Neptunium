using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Model
{
    [DataContract]
    public class ArtistData : ModelBase
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string AlternateName { get; set; }
        [DataMember]
        public string ArtistID { get; set; }
        [DataMember]
        public string Gender { get; set; }
        [DataMember]
        public string ArtistImage { get; set; }
        [DataMember]
        public string ArtistLinkUrl { get; set; }
        [DataMember]
        public string WikipediaUrl { get; internal set; }
    }
}
