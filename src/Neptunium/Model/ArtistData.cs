using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Data
{
    public class ArtistData : ModelBase
    {
        public string Name { get; internal set; }
        public string ArtistID { get; internal set; }
        public string Gender { get; internal set; }
        public string ArtistImage { get; internal set; }
    }
}
