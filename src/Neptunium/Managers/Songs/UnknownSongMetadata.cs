namespace Neptunium.Managers.Songs
{
    public class UnknownSongMetadata : SongMetadata
    {
        public override string Artist { get { return "Unknown Artist"; } }
        public override string Track { get { return "Unknown Song"; } }

        public override MusicBrainzSongMetadata MBData { get { return null; } }
        public override ITunesSongMetadata ITunesData { get { return null; } }
    }
}