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
    public struct SongHistoryItem
    {
        public string Track { get; set; }
        public string Artist { get; set; }
        public string StationPlayedOn { get; set; }

        public DateTime PlayedDate { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Track, Artist);
        }
    }

    [DataContract]
    public class OldSongHistoryItem
    {
        [DataMember]
        public SongMetadata Metadata { get; set; }
        [DataMember]
        public DateTime PlayedDate { get; set; }
    }
}
