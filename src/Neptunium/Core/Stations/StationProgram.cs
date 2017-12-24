using System;
using System.Collections.Generic;

namespace Neptunium.Core.Stations
{
    public class StationProgram
    {
        public string Host { get; set; }
        public string HostRegexExpression { get; set; }
        public string Name { get; set; }
        public StationProgramTimeListing[] TimeListings { get; internal set; }
        public StationItem Station { get; internal set; }
    }

    public class StationProgramTimeListing
    {
        public DateTime Time { get; internal set; }
        public string Day { get; internal set; }
    }
}