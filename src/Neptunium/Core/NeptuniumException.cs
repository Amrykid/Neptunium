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
        public NeptuniumException(Exception inner = null): base("", inner) { }
    }

    public class NeptuniumNetworkConnectionRequiredException: NeptuniumException
    {
        public override string Message
        {
            get
            {
                return "An internet connection is required to do this.";
            }
        }
    }

    public class NeptuniumStreamConnectionFailedException: NeptuniumException
    {
        private string _message = null;

        public StationStream Stream { get; private set; }

        public NeptuniumStreamConnectionFailedException(StationStream stream, Exception inner = null): base(inner)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            Stream = stream;

            _message = string.Format("We were unable to stream {0} for some reason.", Stream.SpecificTitle);
        }

        public NeptuniumStreamConnectionFailedException(StationStream stream, string message, Exception inner = null) : base(inner)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            Stream = stream;
            _message = message;
        }

        public override string Message { get { return _message; } }
    }
}
