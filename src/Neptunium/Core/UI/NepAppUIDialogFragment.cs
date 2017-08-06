using Crystal3.Model;
using System.Threading.Tasks;

namespace Neptunium.Core.UI
{
    public abstract class NepAppUIDialogFragment: ViewModelFragment
    {
        public TaskCompletionSource<NepAppUIManagerDialogResult> ResultTaskCompletionSource { get; protected set; }
        public abstract Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter);
    }
}