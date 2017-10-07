using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View.Dialog
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProgressIndicatorDialog : Page
    {
        public ProgressIndicatorDialog()
        {
            this.InitializeComponent();
        }

        public void SetIndeterminate()
        {
            PART_ProgressIndicator.IsIndeterminate = true;
        }

        public void SetDeterminateProgress(double value)
        {
            PART_ProgressIndicator.IsIndeterminate = false;
            PART_ProgressIndicator.Value = value;
            PART_ProgressIndicator.Maximum = 1.0;
        }

        public void SetTitleAndMessage(string title, string message)
        {
            PART_TitleBlock.Text = title;
            PART_MessageBlock.Text = message;
        }
    }
}
