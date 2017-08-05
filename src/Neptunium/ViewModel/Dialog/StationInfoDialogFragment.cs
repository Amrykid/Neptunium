using Neptunium.Core.Stations;
using Neptunium.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel.Dialog
{
    public class StationInfoDialogFragment: NepAppUIDialogFragment
    {
        private TaskCompletionSource<NepAppUIManagerDialogResult> resultTask = null;

        public StationItem Station { get { return GetPropertyValue<StationItem>(); } private set { SetPropertyValue<StationItem>(value: value); } }

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            if (parameter == null || !(parameter is StationItem)) Task.FromResult<NepAppUIManagerDialogResult>(NepAppUIManagerDialogResult.Declined);

            Station = parameter as StationItem;

            resultTask = new TaskCompletionSource<NepAppUIManagerDialogResult>();

            return resultTask.Task;
        }
    }
}
