namespace Neptunium.Managers.Songs
{
    public class SongMetadata
    {
        internal SongMetadata()
        {
        }

        public object Album { get; internal set; }
        public string Artist { get; internal set; }
        public string Track { get; internal set; }
        public MusicBrainzSongMetadata MBData { get; internal set; }
    }
}