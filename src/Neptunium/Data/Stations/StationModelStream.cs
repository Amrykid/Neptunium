namespace Neptunium.Data
{
    public class StationModelStream
    {
        internal StationModelStream()
        {

        }

        public int Bitrate { get; internal set; }
        public string ContentType { get; internal set; }
        public string HistoryPath { get; internal set; }
        public string RelativePath { get; internal set; }
        public uint SampleRate { get; internal set; }
        public StationModelStreamServerType ServerType { get; internal set; }
        public string Url { get; internal set; }
    }

    public enum StationModelStreamServerType
    {
        Direct = 0,
        Shoutcast = 1,
        Icecast = 1,
        Other = 2
    }
}