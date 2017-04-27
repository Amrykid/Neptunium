using System;

namespace Neptunium.Core.Stations
{
    public class StationStream
    {
        public virtual string SpecificTitle { get { return ParentStation?.Name; } }
        public StationItem ParentStation { get; private set; }
        public Uri StreamUrl { get; private set; }
        public StationStreamServerFormat ServerFormat { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} [url: {1} ]", SpecificTitle, StreamUrl?.ToString());
        }
    }
}