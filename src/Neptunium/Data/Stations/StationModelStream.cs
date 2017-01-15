using System.Runtime.Serialization;

namespace Neptunium.Data
{
    [DataContract]
    public class StationModelStream
    {
        public StationModelStream()
        {

        }

        [DataMember]
        public int Bitrate { get; internal set; }
        [DataMember]
        public string ContentType { get; internal set; }
        [DataMember]
        public string HistoryPath { get; internal set; }
        [DataMember]
        public string RelativePath { get; internal set; }
        [DataMember]
        public uint SampleRate { get; internal set; }
        [DataMember]
        public StationModelStreamServerType ServerType { get; internal set; }
        [DataMember]
        public string Url { get; internal set; }
        [DataMember]
        public int ChannelCount { get; internal set; } = 2;
    }

    public enum StationModelStreamServerType
    {
        Other = -1,
        Direct = 0,
        Shoutcast = 1,
        Icecast = 2, //very similar to shoutcast
        Radionomy = 3, //shoutcast with quirks on top
    }
}