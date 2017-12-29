using Crystal3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptunium.Core.Stations;

namespace Neptunium.Model
{
    public class ScheduleItem : ModelBase
    {
        public StationItem Station { get; internal set; }
        public string Day { get; internal set; }
        public TimeSpan Time { get { return TimeLocal.TimeOfDay; } }
        public DateTime TimeLocal { get; internal set; }
        public StationProgram Program { get; internal set; }
    }
}
