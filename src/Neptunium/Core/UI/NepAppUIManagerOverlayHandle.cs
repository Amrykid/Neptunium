using Crystal3.Model;
using Crystal3.Navigation;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
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
            inlineFrame.Height = 400;
            inlineFrame.Width = 300;

            overlayGridControl.Children.Add(inlineFrame);

            overlayGridControl.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            overlayGridControl.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            overlayGridControl.IsHitTestVisible = true;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
            inlineFrame.Focus(Windows.UI.Xaml.FocusState.Pointer);

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
