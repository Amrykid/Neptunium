using Crystal3.Navigation;
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
using Crystal3.Model;
using Neptunium.ViewModel;
using Neptunium.Services.SnackBar;
using Crystal3.InversionOfControl;
using System.Threading;
using Neptunium.Media;
using Neptunium.View.Fragments;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View.Xbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [NavigationViewModel(typeof(ViewModel.AppShellViewModel), NavigationViewSupportedPlatform.Xbox)]
    public sealed partial class XboxAppShellView : Page
    {
        private XboxAppShellViewPivotNavigationService inlineNavService = null;
        public XboxAppShellView()
        {
            this.InitializeComponent();

            IoC.Current.Register<ISnackBarService>(new SnackBarService(floatingSnackBarGrid));

            inlineNavService = new XboxAppShellViewPivotNavigationService(mainPivot, (Type auxViewModel) =>
            {
                if (auxViewModel == typeof(StationInfoViewModel))
                    return "Station Info";

                return null;
            });
            WindowManager.GetNavigationManagerForCurrentWindow()
                .RegisterCustomNavigationService(inlineNavService);
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            inlineNavService.NavigateTo<StationsViewViewModel>();

            inlineNavService.ClearBackStack();
        }


        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Crystal3.CrystalApplication.GetDevicePlatform() == Crystal3.Core.Platform.Xbox)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.GamepadY:
                        if (StationMediaPlayer.IsPlaying)
                            (lowerAppBar.Content as NowPlayingInfoBar)?.ShowHandoffFlyout();
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.GamepadX:
                        {
                            //mimic's groove music uwp on xbox one

                            if (!inlineNavService.IsNavigatedTo<NowPlayingViewViewModel>())
                                inlineNavService.NavigateTo<NowPlayingViewViewModel>();
                            else if (inlineNavService.CanGoBackward)
                                inlineNavService.GoBack();

                            e.Handled = true;
                        }
                        break;

                }
            }
        }

        private class XboxAppShellViewPivotNavigationService : NavigationServiceBase
        {
            private Pivot pivotControl = null;
            private ViewModelBase currentViewModel = null;
            private PivotItem auxillaryPivotItem = null;
            private Frame auxillaryPivotItemFrame = null;
            private FrameNavigationService auxillaryPivotItemNavService = null;
            private Func<Type, string> auxillaryViewModelNameCallback = null;

            public XboxAppShellViewPivotNavigationService(Pivot mainPivot, Func<Type, string> auxViewModelNameCallback)
            {
                navigationLock = new ManualResetEvent(true);
                viewModelBackStack = new Stack<ViewModelBase>();
                viewModelForwardStack = new Stack<ViewModelBase>();

                auxillaryViewModelNameCallback = auxViewModelNameCallback;

                pivotControl = mainPivot;

                pivotControl.SelectionChanged += PivotControl_SelectionChanged;

                auxillaryPivotItem = new PivotItem();
                auxillaryPivotItem.Header = "Aux";

                auxillaryPivotItemFrame = new Frame();

                auxillaryPivotItemNavService = new FrameNavigationService(auxillaryPivotItemFrame);
                auxillaryPivotItemNavService.NavigationServicePreNavigatedSignaled += AuxillaryPivotItemNavService_NavigationServicePreNavigatedSignaled;

                auxillaryPivotItem.Content = auxillaryPivotItemFrame;
            }

            private void AuxillaryPivotItemNavService_NavigationServicePreNavigatedSignaled(object sender, NavigationServicePreNavigatedSignaledEventArgs e)
            {
                string result = auxillaryViewModelNameCallback?.Invoke(e.ViewModel.GetType());
                if (string.IsNullOrWhiteSpace(result))
                    result = "Aux";

                auxillaryPivotItem.Header = result;
            }

            private void PivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                var newPivotItem = e.AddedItems[0] as PivotItem;
                if (newPivotItem != null)
                {
                    navigationLock.WaitOne();

                    if (newPivotItem.DataContext is ViewModelBase && newPivotItem.DataContext != pivotControl.DataContext)
                    {
                        if (currentViewModel != null)
                            viewModelBackStack.Push(currentViewModel);

                        var viewModel = ((ViewModelBase)newPivotItem.DataContext);

                        FireViewModelNavigatedToEvent(viewModel, new CrystalNavigationEventArgs() { Direction = CrystalNavigationDirection.Refresh });

                        currentViewModel = viewModel;
                    }
                    else
                    {
                        var viewModelString = newPivotItem.GetValue(NavigationAttributes.NavigationHintProperty) as string;

                        if (!string.IsNullOrWhiteSpace(viewModelString))
                        {
                            var viewModelType = Type.GetType(viewModelString);
                            PivotNavigate(viewModelType, newPivotItem);
                        }
                    }

                    navigationLock.Set();
                }
            }

            public override bool CanGoBackward { get { return (viewModelBackStack?.Count > 0) || auxillaryPivotItemNavService.CanGoBackward || auxillaryPivotItemFrame.Content != null; } }

            public override void ClearBackStack()
            {
                viewModelBackStack.Clear();
            }

            public override ViewModelBase GetNavigatedViewModel()
            {
                return currentViewModel;
            }

            public override void GoBack()
            {
                navigationLock.WaitOne();

                pivotControl.SelectionChanged -= PivotControl_SelectionChanged;

                if (auxillaryPivotItemNavService.CanGoBackward)
                {
                    auxillaryPivotItemNavService.GoBack();
                }
                else
                {
                    HandleNavigateFromAuxPivotItem();

                    if (CanGoBackward)
                    {
                        var backViewModel = viewModelBackStack.Pop();

                        PivotItem requiredPivot = (PivotItem)pivotControl.Items.FirstOrDefault(x =>
                            (string)((PivotItem)x).GetValue(NavigationAttributes.NavigationHintProperty) == backViewModel.GetType().FullName);

                        if (requiredPivot != null)
                        {
                            //top level viewmodel, just switch pivots

                            pivotControl.SelectedItem = requiredPivot;

                            if (requiredPivot.DataContext is ViewModelBase)
                            {
                                ViewModelBase viewModel = requiredPivot.DataContext as ViewModelBase;
                                FireViewModelNavigatedToEvent(viewModel, new CrystalNavigationEventArgs() { Direction = CrystalNavigationDirection.Backward });

                                currentViewModel = viewModel;
                            }
                        }
                    }
                }

                pivotControl.SelectionChanged += PivotControl_SelectionChanged;
                navigationLock.Set();
            }

            private void HandleNavigateFromAuxPivotItem()
            {
                if (pivotControl.Items.Contains(auxillaryPivotItem))
                {
                    pivotControl.Items.Remove(auxillaryPivotItem);

                    auxillaryPivotItemFrame.Content = null;

                    var viewModel = auxillaryPivotItemNavService.GetNavigatedViewModel();

                    if (viewModel is UIViewModelBase)
                        SetViewModelUIElement(((UIViewModelBase)viewModel), null);

                    FireViewModelNavigatedFromEvent(viewModel, new CrystalNavigationEventArgs() { Direction = CrystalNavigationDirection.Backward });
                }

                pivotControl.IsLocked = false;
            }

            public override bool IsNavigatedTo(Type viewModelType)
            {
                return GetNavigatedViewModel()?.GetType() == viewModelType;
            }

            public override bool IsNavigatedTo<T>()
            {
                return IsNavigatedTo(typeof(T));
            }

            public override void NavigateTo<T>(object parameter = null)
            {
                navigationLock.WaitOne();

                pivotControl.SelectionChanged -= PivotControl_SelectionChanged;

                Type viewModelType = typeof(T);

                PivotItem requiredPivot = (PivotItem)pivotControl.Items.FirstOrDefault(x =>
                    (string)((PivotItem)x).GetValue(NavigationAttributes.NavigationHintProperty) == viewModelType.FullName);

                if (requiredPivot != null)
                {
                    HandleNavigateFromAuxPivotItem();

                    PivotNavigate(viewModelType, requiredPivot);
                }
                else
                {
                    if (currentViewModel != null)
                        viewModelBackStack.Push(currentViewModel);

                    //we gotta actually navigate to it.

                    if (!pivotControl.Items.Contains(auxillaryPivotItem))
                    {
                        pivotControl.Items.Add(auxillaryPivotItem);

                        pivotControl.SelectedIndex = pivotControl.IndexFromContainer(auxillaryPivotItem);

                        pivotControl.IsLocked = true;
                    }

                    auxillaryPivotItemNavService.SafeNavigateTo<T>(parameter);

                    currentViewModel = auxillaryPivotItemNavService.GetNavigatedViewModel();
                }

                pivotControl.SelectionChanged += PivotControl_SelectionChanged;

                navigationLock.Set();
            }

            private void PivotNavigate(Type viewModelType, PivotItem requiredPivot)
            {
                if (currentViewModel != null)
                    viewModelBackStack.Push(currentViewModel);

                //top level viewmodel, just switch pivots

                pivotControl.SelectedIndex = pivotControl.IndexFromContainer(requiredPivot);

                if (requiredPivot.DataContext == null || requiredPivot.DataContext == pivotControl.DataContext)
                {
                    //instantiate the view model associated with this pivot

                    ViewModelBase viewModel = (ViewModelBase)Activator.CreateInstance(viewModelType);

                    if (viewModel is UIViewModelBase)
                        SetViewModelUIElement(((UIViewModelBase)viewModel), requiredPivot);

                    requiredPivot.DataContext = viewModel;

                    if (requiredPivot.Content == null)
                    {
                        var viewType = GetViewType(viewModelType);
                        var content = Activator.CreateInstance(viewType) as FrameworkElement;
                        content.DataContext = viewModel;
                        requiredPivot.Content = content;
                    }
                }

                if (requiredPivot.DataContext is ViewModelBase)
                {
                    ViewModelBase viewModel = requiredPivot.DataContext as ViewModelBase;
                    FireViewModelNavigatedToEvent(viewModel, new CrystalNavigationEventArgs() { Direction = CrystalNavigationDirection.Forward });

                    currentViewModel = viewModel;
                }
            }
        }
    }
}
