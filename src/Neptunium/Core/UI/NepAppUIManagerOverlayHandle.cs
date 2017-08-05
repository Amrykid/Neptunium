using Crystal3.Model;
using Crystal3.Navigation;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Neptunium.NepApp;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerOverlayHandle : IDisposable
    {
        private SemaphoreSlim overlayLock;
        private NepAppUIManager parentUIManager;
        private Grid overlayGridControl;
        private Frame inlineFrame = null;
        internal NepAppUIManagerOverlayHandle(NepAppUIManager parent, Grid overlayControl)
        {
            parentUIManager = parent;
            overlayGridControl = overlayControl;

            overlayLock = new SemaphoreSlim(1);
            inlineFrame = new Frame();

            inlineFrame.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
            inlineFrame.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;

            //todo handle orientation, etc
            Window.Current.SizeChanged += Current_SizeChanged;
            ResizeInlineFrameDialog(Window.Current.Bounds.Height, Window.Current.Bounds.Width);

            overlayGridControl.Children.Add(inlineFrame);

            overlayGridControl.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            overlayGridControl.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            overlayGridControl.IsHitTestVisible = true;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            ResizeInlineFrameDialog(e.Size.Height, e.Size.Width);
        }

        private void ResizeInlineFrameDialog(double height, double width)
        {
            if (width >= 720)
            {
                inlineFrame.Width = 400;
                inlineFrame.Height = 600;
            }
            else
            {
                inlineFrame.Width = width;
                inlineFrame.Height = height;
            }
        }

        public void Dispose()
        {
            ((IDisposable)overlayLock).Dispose();
        }

        public async Task<NepAppUIManagerDialogResult> ShowDialogFragmentAsync<T>(object parameter = null) where T : NepAppUIDialogFragment
        {
            await overlayLock.WaitAsync();

            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Visible;


            var fragment = Activator.CreateInstance<T>() as NepAppUIDialogFragment;

            var viewType = FragmentManager.ResolveFragmentView<T>();
            var view = Activator.CreateInstance(viewType) as Control;

            view.DataContext = fragment;

            inlineFrame.Content = view;
            inlineFrame.BorderBrush = new SolidColorBrush((Color)view.Resources["SystemAccentColor"]);
            inlineFrame.BorderThickness = new Thickness(1.5);
            inlineFrame.Focus(Windows.UI.Xaml.FocusState.Pointer);

            //todo handle escape button and the back button

            var result = await fragment.InvokeAsync(parameter);

            inlineFrame.Content = null;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            overlayLock.Release();

            return result;
        }

        public void RegisterDialogFragment<T, V>() where T : NepAppUIDialogFragment where V : Control
        {
            FragmentManager.RegisterFragmentView<T, V>();
        }
    }
}
