using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Stations
{
    public class StationItem: ModelBase
    {
        public StationItem(string name, string description, Uri stationLogo, StationStream[] streams)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (stationLogo == null) throw new ArgumentNullException(nameof(stationLogo));

            Name = name;
            Description = description;
            StationLogoUrl = stationLogo;

            foreach(var stream in streams)
            {
                if (stream.ParentStation != this)
                    throw new Exception(string.Format("{0} stream's parent station doesn't match {1}", stream.ToString(), Name));

                if (stream.StreamUrl == null) throw new Exception(string.Format("{0} stream doesn't have a url.", stream.ToString()));
            }

            Streams = streams;
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public Uri StationLogoUrl { get; private set; }

        public StationStream[] Streams { get; private set; }
    }

    //Used in cases where one provider (e.g. asia dream radio) has multiple different streams under their name.
    //public class BrandGroupStationItem: StationItem
    //{

    //}
}
