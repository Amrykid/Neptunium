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

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (obj is SongMetadata)
            {
                var other = (SongMetadata)obj;

                return this.Artist.Equals(other.Artist) && this.Track.Equals(other.Track);
            }

            return base.Equals(obj);
        }
    }
}