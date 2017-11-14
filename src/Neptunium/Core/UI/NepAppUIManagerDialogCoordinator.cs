using Crystal3.Model;
using Crystal3.Navigation;
using Kimono.Controls.SnackBar;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Neptunium.View.Dialog;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Neptunium.NepApp;

namespace Neptunium.Core.UI
{
    public class NepAppUIManagerDialogCoordinator : IDisposable
    {
        private SemaphoreSlim overlayLock;
        private NepAppUIManager parentUIManager;
        private Grid overlayGridControl;
        private Frame inlineFrame = null;
        private SnackBarManager snackManager = null;

        private FadeInThemeAnimation fadeInAnimation = null;
        private FadeOutThemeAnimation fadeOutAnimation = null;

        public bool IsOverlayedDialogVisible { get; private set; }

        public event EventHandler OverlayedDialogShown;
        public event EventHandler OverlayedDialogHidden;

        internal NepAppUIManagerDialogCoordinator(NepAppUIManager parent, Grid overlayControl, Grid snackBarContainer)
        {
            parentUIManager = parent;
            overlayGridControl = overlayControl;

            snackManager = new SnackBarManager(snackBarContainer);

            fadeInAnimation = new FadeInThemeAnimation();
            fadeOutAnimation = new FadeOutThemeAnimation();

            InitializeOverlayAndOverlayedDialogs();
        }

        private void InitializeOverlayAndOverlayedDialogs()
        {
            overlayLock = new SemaphoreSlim(1);
            inlineFrame = new Frame();

            inlineFrame.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
            inlineFrame.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;

            inlineFrame.ContentTransitions = new TransitionCollection();
            //inlineFrame.ContentTransitions.Add(new AddDeleteThemeTransition());
            //inlineFrame.ContentTransitions.Add(new ContentThemeTransition());

            //todo handle orientation, etc
            //ApplicationView.GetForCurrentView().VisibleBoundsChanged += NepAppUIManagerOverlayHandle_VisibleBoundsChanged;
            //Window.Current.SizeChanged += Current_SizeChanged;

            overlayGridControl.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            overlayGridControl.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            overlayGridControl.IsHitTestVisible = true;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //overlayGridControl.ChildrenTransitions.Add(new EntranceThemeTransition());

            overlayGridControl.Transitions = new TransitionCollection();
            //overlayGridControl.Transitions.Add(new PaneThemeTransition());
        }

        private void NepAppUIManagerOverlayHandle_VisibleBoundsChanged(ApplicationView sender, object args)
        {
            var visibleScreen = GetScreenBounds();
            ResizeInlineFrameDialog(visibleScreen.Height, visibleScreen.Width);
        }

        private Rect GetScreenBounds()
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Mobile 
                || UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Touch)
            {
                return Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;
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
                var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                if (appView.DesiredBoundsMode == ApplicationViewBoundsMode.UseCoreWindow)
                {
                    //usually this is used on Xbox
                    inlineFrame.Width = 400;
                    inlineFrame.Height = height - 35;
                }
                else
                {
                    inlineFrame.Width = 400;
                    inlineFrame.Height = 600;
                }
            }
            else
            {
                inlineFrame.Width = width;
                inlineFrame.Height = height - 20;

            }
        }

        public void Dispose()
        {
            ((IDisposable)overlayLock).Dispose();
        }

        public async Task<NepAppUIManagerDialogResult> ShowDialogFragmentAsync<T>(object parameter = null) where T : NepAppUIDialogFragment
        {
            await BeginShowingDialogAsync();

            //used to handle resizing the dialog
            ApplicationView appView = ApplicationView.GetForCurrentView();
            appView.VisibleBoundsChanged += NepAppUIManagerOverlayHandle_VisibleBoundsChanged;
            Window.Current.SizeChanged += Current_SizeChanged;

            Rect bounds = GetScreenBounds();
            ResizeInlineFrameDialog(bounds.Height, bounds.Width);

            var fragment = Activator.CreateInstance<T>() as NepAppUIDialogFragment;

            var viewType = FragmentManager.ResolveFragmentView<T>();
            var view = Activator.CreateInstance(viewType) as Control;

            view.DataContext = fragment;

            inlineFrame.Content = view;

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

            var navManager = WindowManager.GetNavigationManagerForCurrentWindow().GetNavigationServiceFromFrameLevel(FrameLevel.Two);
            EventHandler<NavigationManagerPreBackRequestedEventArgs> backHandler = null;
            bool backHandlerReleased = false;
            backHandler = new EventHandler<NavigationManagerPreBackRequestedEventArgs>((o, e) =>
            {
                //hack to handle the back button.
                e.Handled = true;
                navManager.PreBackRequested -= backHandler;
                backHandlerReleased = true;
                fragment.ResultTaskCompletionSource.SetResult(NepAppUIManagerDialogResult.Declined);
            });
            navManager.PreBackRequested += backHandler;

            var result = await fragment.InvokeAsync(parameter);

            if (!backHandlerReleased)
            {
                navManager.PreBackRequested -= backHandler;
            }

            if (!escapeHandlerReleased)
            {
                view.KeyDown -= escapeHandler;
            }

            appView.VisibleBoundsChanged -= NepAppUIManagerOverlayHandle_VisibleBoundsChanged;
            Window.Current.SizeChanged -= Current_SizeChanged;

            await EndShowingDialogAsync();

            return result;
        }

        private async Task EndShowingDialogAsync()
        {
            await overlayGridControl.Fade(0).StartAsync();

            inlineFrame.Content = null;
            overlayGridControl.Children.Remove(inlineFrame);
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            IsOverlayedDialogVisible = false;
            OverlayedDialogHidden?.Invoke(this, EventArgs.Empty);
            overlayLock.Release();
        }

        private async Task BeginShowingDialogAsync()
        {
            await overlayLock.WaitAsync();

            IsOverlayedDialogVisible = true;
            overlayGridControl.Opacity = 0;
            overlayGridControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
            await overlayGridControl.Fade(.95f).StartAsync();

            OverlayedDialogShown?.Invoke(this, EventArgs.Empty);

            inlineFrame.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            inlineFrame.BorderThickness = new Thickness(1.5);

            overlayGridControl.Children.Add(inlineFrame);
            inlineFrame.Focus(Windows.UI.Xaml.FocusState.Pointer);

        }

        public async Task<NepAppUIManagerDialogController> ShowProgressDialogAsync(string title, string message)
        {
            await BeginShowingDialogAsync();

            inlineFrame.Width = double.NaN;
            inlineFrame.Height = double.NaN;
            inlineFrame.BorderThickness = new Thickness(0);

            //no-mvvm
            var dialog = new ProgressIndicatorDialog();
            dialog.SetTitleAndMessage(title, message);
            inlineFrame.Content = dialog;

            return new NepAppUIManagerDialogController(dialog, async () =>
            {
                if (NepApp.UI.Overlay.IsOverlayedDialogVisible)
                {
                    await EndShowingDialogAsync();
                }
            });
        }

        public class NepAppUIManagerDialogController
        {
            private Func<Task> endingCallback = null;
            private bool closed = false;
            private ProgressIndicatorDialog dialog = null;
            internal  NepAppUIManagerDialogController(ProgressIndicatorDialog openedDialog, Func<Task> callback)
            {
                if (!NepApp.UI.Overlay.IsOverlayedDialogVisible) throw new InvalidOperationException();

                dialog = openedDialog;
                endingCallback = callback;
            }

            public bool IsIndeterminate { get; private set; }

            public void SetIndeterminate()
            {
                IsIndeterminate = true;
                dialog.SetIndeterminate();
            }

            public void SetDeterminateProgress(double value)
            {
                if (value > 1.0 || value < 0.0) throw new ArgumentOutOfRangeException(nameof(value));

                IsIndeterminate = false;
                dialog.SetDeterminateProgress(value);
            }

            public async Task CloseAsync()
            {
                if (closed) return;

                if (NepApp.UI.Overlay.IsOverlayedDialogVisible)
                {
                    await endingCallback?.Invoke();
                    closed = true;
                }
            }
        }

        public Task ShowSnackBarMessageAsync(string message)
        {
            return ShowSnackBarMessageAsync(message, TimeSpan.FromSeconds(5));
        }
        public Task ShowSnackBarMessageAsync(string message, TimeSpan duration)
        {
            return snackManager.ShowMessageAsync(message, (int)duration.TotalMilliseconds);  
        }

        public void RegisterDialogFragment<T, V>() where T : NepAppUIDialogFragment where V : Control
        {
            FragmentManager.RegisterFragmentView<T, V>();
        }
    }
}
