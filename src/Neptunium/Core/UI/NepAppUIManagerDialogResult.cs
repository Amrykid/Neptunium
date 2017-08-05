using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerDialogResult
    {
        public static NepAppUIManagerDialogResult Declined => new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResultType.Negative };

        public enum NepAppUIManagerDialogResultType
        {
            Negative = 0,
            Positive = 1
        }

        public NepAppUIManagerDialogResultType ResultType { get; set; }
        public object Selection { get; set; }

        public NepAppUIManagerDialogResult()
        {

        }
    }
}
