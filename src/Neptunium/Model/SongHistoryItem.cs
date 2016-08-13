using System;
using Crystal3.Model;
using Neptunium.Data;
using System.Runtime.Serialization;

namespace Neptunium.Managers
{
    [DataContract]
    public class SongHistoryItem: ModelBase
    {
        public SongHistoryItem()
        {

        }

        [DataMember]
        public AlbumData Album
        {
            get { return GetPropertyValue<AlbumData>(); }
            set { SetPropertyValue<AlbumData>(value: value); }
        }

        [DataMember]
        public string Artist { get; internal set; }
        [DataMember]
        public DateTime DatePlayed { get; internal set; }
        [DataMember]
        public string Station { get; internal set; }
        [DataMember]
        public string Track { get; internal set; }
    }
}