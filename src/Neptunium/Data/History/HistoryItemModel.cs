using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Data.History
{
    public class HistoryItemModel
    {
        internal HistoryItemModel()
        { }

        public string Song { get; internal set; }
        public string Time { get; internal set; }
    }
}
