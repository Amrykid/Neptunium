using Crystal3.Model;
using Neptunium.Core.Media.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.History
{
    [DataContract]
    public class SongHistoryItem
    {
        [DataMember]
        public SongMetadata Metadata { get; set; }
        [DataMember]
        public DateTime PlayedDate { get; set; }
    }
}
