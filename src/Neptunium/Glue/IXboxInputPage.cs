using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Neptunium.Glue
{
    public interface IXboxInputPage
    {
        void SetLeftFocus(UIElement elementToTheLeft);
        void SetRightFocus(UIElement elementToTheRight);
        void SetTopFocus(UIElement elementAbove);
        void SetBottomFocus(UIElement elementBelow);

        void RestoreFocus();
        void PreserveFocus();

        void FocusDefault();
    }
}
