using Neptunium.Core.Stations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core
{
    public abstract class NeptuniumException: Exception
    {
    }

    public class NeptuniumNetworkConnectionRequiredException: Exception
    {
        public override string Message
        {
            get
            {
                return "An internet connection is required to do this.";
            }
        }
    }

    public class NeptuniumStreamConnectionFailedException: Exception
    {
        public StationStream Stream { get; private set; }

        public NeptuniumStreamConnectionFailedException(StationStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            Stream = stream;
        }

        public override string Message
        {
            get
            {
                return string.Format("We were unable to stream {0} for some reason.", Stream.SpecificTitle);
            }
        }
    }
}
