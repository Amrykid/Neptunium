using Crystal3.Model;
using System.Threading.Tasks;

namespace Neptunium.Core.UI
{
    public abstract class NepAppUIDialogFragment: ViewModelFragment
    {
        public abstract Task<NepAppUIManagerDialogResult> InvokeAsync(object parameter);
    }
}