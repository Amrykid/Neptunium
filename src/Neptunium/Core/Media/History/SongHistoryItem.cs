using Crystal3.Model;
using Neptunium.Core.Media.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.Media.History
{
    public class SongHistoryItem: ModelBase
    {
        public SongMetadata Metadata { get { return GetPropertyValue<SongMetadata>(); } set { SetPropertyValue<SongMetadata>(value: value); } }
        public DateTime PlayedDate { get { return GetPropertyValue<DateTime>(); } set { SetPropertyValue<DateTime>(value: value); } }
    }
}
