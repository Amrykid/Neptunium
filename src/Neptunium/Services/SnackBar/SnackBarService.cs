using Kimono.Controls.SnackBar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Neptunium.Services.SnackBar
{
    public class SnackBarService : ISnackBarService
    {
        private SnackBarManager Manager { get; set; }
        public SnackBarService(Grid desiredSnackBarArea)
        {
            Manager = new SnackBarManager(desiredSnackBarArea);
        }
        public async Task ShowSnackAsync(string text, int msToShow = 5000)
        {
            await Manager.ShowMessageAsync(text, msToShow);
        }

        public async Task ShowActionableSnackAsync(string text, string buttonText, Action<object> callback, int msToShow = 5000)
        {
            await Manager.ShowMessageWithCallbackAsync(text, buttonText, callback, msToShow);
        }
    }
}
