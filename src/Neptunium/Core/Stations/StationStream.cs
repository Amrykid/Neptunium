using System;

namespace Neptunium.Core.Stations
{
    public class StationStream
    {
        public StationStream(Uri url)
        {
            StreamUrl = url;
        }

        public virtual string SpecificTitle { get { return ParentStation?.Name; } }
        public StationItem ParentStation { get; internal set; }
        public Uri StreamUrl { get; private set; }
        public StationStreamServerFormat ServerFormat { get; private set; }
        public string ContentType { get; internal set; }
        public int Bitrate { get; internal set; }
        public string RelativePath { get; internal set; }
        public StationStreamServerFormat ServerType { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} [url: {1} ]", SpecificTitle, StreamUrl?.ToString());
        }
    }
}