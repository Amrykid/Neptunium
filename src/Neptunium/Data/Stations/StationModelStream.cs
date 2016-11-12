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
    }

    public enum StationModelStreamServerType
    {
        Direct = 0,
        Shoutcast = 1,
        Icecast = 1,
        Other = 2,
        Radionomy = 3
    }
}