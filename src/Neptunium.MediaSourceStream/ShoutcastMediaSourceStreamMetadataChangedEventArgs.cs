using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.MediaSourceStream
{
    public class ShoutcastMediaSourceStreamMetadataChangedEventArgs: EventArgs
    {
        internal ShoutcastMediaSourceStreamMetadataChangedEventArgs()
        {

        }

        public string Artist { get; internal set; }
        public string Title { get; internal set; }
    }
}
