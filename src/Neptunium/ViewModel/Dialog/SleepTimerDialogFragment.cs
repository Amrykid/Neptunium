using Crystal3.UI.Commands;
using Neptunium.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.ViewModel.Dialog
{
    public class SleepTimerDialogFragment : NepAppUIDialogFragment
    {
        public SleepTimerDialogFragment()
        {
            ResultTaskCompletionSource = new TaskCompletionSource<NepAppUIManagerDialogResult>();
        }

        public RelayCommand CancelCommand => new RelayCommand(x => ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined));
        public RelayCommand PlayCommand => new RelayCommand(x =>
            ResultTaskCompletionSource.SetResult(new NepAppUIManagerDialogResult() { ResultType = NepAppUIManagerDialogResult.NepAppUIManagerDialogResultType.Positive }));

        public override Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter)
        {
            return ResultTaskCompletionSource.Task;
        }
    }
}
