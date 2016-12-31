using System.Runtime.Serialization;

namespace Neptunium.Managers.Songs
{
    [DataContract]
    public class SongMetadata
    {
        internal SongMetadata()
        {
        }

        [DataMember]
        public virtual string Artist { get; set; }
        [DataMember]
        public virtual string Track { get; set; }
        [DataMember]
        public virtual MusicBrainzSongMetadata MBData { get; set; }
        [DataMember]
        public virtual ITunesSongMetadata ITunesData { get; set; }

        public override int GetHashCode()
        {
            return Artist.GetHashCode() + Track.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(" - ", Artist, Track);
        }
    }
}