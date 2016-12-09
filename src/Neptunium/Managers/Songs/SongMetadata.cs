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
        public string Artist { get; set; }
        [DataMember]
        public string Track { get; set; }
        [DataMember]
        public MusicBrainzSongMetadata MBData { get; set; }
        [DataMember]
        public ITunesSongMetadata ITunesData { get; set; }

        public override int GetHashCode()
        {
            return Artist.GetHashCode() + Track.GetHashCode();
        }
    }
}