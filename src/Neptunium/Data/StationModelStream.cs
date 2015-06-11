namespace Neptunium.Data
{
    public class StationModelStream
    {
        internal StationModelStream()
        {

        }

        public int Bitrate { get; internal set; }
        public string ContentType { get; internal set; }
        public string RelativePath { get; internal set; }
        public uint SampleRate { get; internal set; }
        public string Url { get; internal set; }
    }
}