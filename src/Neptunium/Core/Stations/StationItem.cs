using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Stations
{
    public class StationItem
    {
        public StationItem(string name, string description, Uri stationLogo, StationStream[] streams)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (stationLogo == null) throw new ArgumentNullException(nameof(stationLogo));

            Name = name;
            Description = description;
            StationLogoUrl = stationLogo;

            foreach (var stream in streams)
            {
                stream.ParentStation = this.Name;

                if (stream.StreamUrl == null) throw new Exception(string.Format("{0} stream doesn't have a url.", stream.ToString()));
            }

            Streams = streams;
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public Uri StationLogoUrl { get; private set; }

        public StationStream[] Streams { get; private set; }
        public StationProgram[] Programs { get; set; }
        public string Background { get; internal set; }
        public string Site { get; internal set; }
        public string[] Genres { get; internal set; }
        public string PrimaryLocale { get; internal set; }
        public string[] StationMessages { get; internal set; }
        public string Group { get; internal set; }
        public Uri StationLogoUrlOnline { get; internal set; }

        public override int GetHashCode()
        {
            return Name.Trim().ToLower().GetHashCode();
        }
    }

    //Used in cases where one provider (e.g. asia dream radio) has multiple different streams under their name.
    //public class BrandGroupStationItem: StationItem
    //{

    //}
}
