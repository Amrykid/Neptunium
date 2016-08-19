using Crystal3.InversionOfControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium.Services.SnackBar
{
    public interface ISnackBarService: IIoCObject
    {
        Task ShowSnackAsync(string text, int msToShow = 5000);
        Task ShowActionableSnackAsync(string text, string buttonText, Action<object> callback, int msToShow = 5000);
    }
}
