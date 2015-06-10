using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Data
{
    public class StationModel : ModelBase
    {
        public string Description { get; internal set; }
        public Uri Logo { get; internal set; }
        public string Name { get; internal set; }

        public IEnumerable<StationModelStream> Streams { get; internal set; }
    }
}
