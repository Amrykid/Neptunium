namespace Neptunium.Media
{
    public class BasicSongInfo
    {
        public string Artist { get; internal set; }
        public string Track { get; internal set; }

        public override string ToString()
        {
            return string.Join(" - ", Artist, Track);
        }
    }
}