using Crystal3.Model;
using Crystal3.Navigation;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
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

            inlineFrame.ContentTransitions = new TransitionCollection();
            inlineFrame.ContentTransitions.Add(new AddDeleteThemeTransition());
            inlineFrame.ContentTransitions.Add(new ContentThemeTransition());

            //todo handle orientation, etc
            Window.Current.SizeChanged += Current_SizeChanged;
            Rect bounds = GetScreenBounds();
            ResizeInlineFrameDialog(bounds.Height, bounds.Width);

            overlayGridControl.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            overlayGridControl.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            overlayGridControl.IsHitTestVisible = true;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            overlayGridControl.ChildrenTransitions.Add(new ContentThemeTransition());
            overlayGridControl.ChildrenTransitions.Add(new AddDeleteThemeTransition());

            overlayGridControl.Transitions = new TransitionCollection();
            overlayGridControl.Transitions.Add(new PaneThemeTransition());
        }

        private Rect GetScreenBounds()
        {
            //todo check if table mode counts. i forgot.
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile)
            {
                if (AreOnScreenNavigationButtonsVisibleOnMobile())
                {
                    return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
                }
            }

            return Window.Current.Bounds;
        }

        private bool AreOnScreenNavigationButtonsVisibleOnMobile()
        {
            //https://social.msdn.microsoft.com/Forums/sqlserver/en-US/dd050898-ef62-4dec-aac4-32a05142931e/on-screen-software-buttons?forum=wpdevelop
            var visible = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
            var window = Window.Current.Bounds;
            return (visible.Height != window.Height || visible.Width != window.Width);
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            var visibleScreen = GetScreenBounds();
            ResizeInlineFrameDialog(visibleScreen.Height, visibleScreen.Width);

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

            overlayGridControl.Children.Add(inlineFrame);

            inlineFrame.Content = view;
            inlineFrame.BorderBrush = new SolidColorBrush((Color)view.Resources["SystemAccentColor"]);
            inlineFrame.BorderThickness = new Thickness(1.5);
            inlineFrame.Focus(Windows.UI.Xaml.FocusState.Pointer);

            KeyEventHandler escapeHandler = null;
            bool escapeHandlerReleased = false;
            escapeHandler = new KeyEventHandler((s, e) =>
            {
                //handles the escape button.
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    view.KeyDown -= escapeHandler;
                    escapeHandlerReleased = true;
                    fragment.ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined);
                }
            });
            view.KeyDown += escapeHandler;

            var navManager = SystemNavigationManager.GetForCurrentView();
            EventHandler<BackRequestedEventArgs> backHandler = null;
            bool backHandlerReleased = false;
            backHandler = new EventHandler<BackRequestedEventArgs>((o, e) =>
            {
                //hack to handle the back button.
                e.Handled = true;
                navManager.BackRequested -= backHandler;
                backHandlerReleased = true;
                fragment.ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined);
            });
            navManager.BackRequested += backHandler;

            var result = await fragment.InvokeAsync(parameter);

            if (!backHandlerReleased)
            {
                navManager.BackRequested -= backHandler;
            }

            if (!escapeHandlerReleased)
            {
                view.KeyDown -= escapeHandler;
            }

            inlineFrame.Content = null;
            overlayGridControl.Children.Remove(inlineFrame);
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
