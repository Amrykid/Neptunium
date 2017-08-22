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
    public class SongHistoryItem: ModelBase
    {
        [DataMember]
        public SongMetadata Metadata { get { return GetPropertyValue<SongMetadata>(); } set { SetPropertyValue<SongMetadata>(value: value); } }
        [DataMember]
        public DateTime PlayedDate { get { return GetPropertyValue<DateTime>(); } set { SetPropertyValue<DateTime>(value: value); } }
    }
}
