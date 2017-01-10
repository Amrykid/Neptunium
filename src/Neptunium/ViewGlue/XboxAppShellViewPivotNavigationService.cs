using Crystal3.Model;
using Crystal3.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Neptunium.ViewGlue
{
    internal class XboxAppShellViewPivotNavigationService : NavigationServiceBase
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

            CreateOrRecyclePivotItemAndNavService();
        }

        private void CreateOrRecyclePivotItemAndNavService()
        {
            if (auxillaryPivotItemNavService != null)
            {
                auxillaryPivotItemNavService.NavigationServicePreNavigatedSignaled -= AuxillaryPivotItemNavService_NavigationServicePreNavigatedSignaled;
                auxillaryPivotItemNavService.Dispose();
                auxillaryPivotItemNavService = null;
            }

            if (auxillaryPivotItemFrame != null)
            {
                if (auxillaryPivotItem.Content == auxillaryPivotItemFrame)
                    auxillaryPivotItem.Content = null;

                auxillaryPivotItemFrame = null;
            }

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

                CreateOrRecyclePivotItemAndNavService();

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

                var viewModel = auxillaryPivotItemNavService.GetNavigatedViewModel();

                if (viewModel is UIViewModelBase)
                    SetViewModelUIElement(((UIViewModelBase)viewModel), null);

                FireViewModelNavigatedFromEvent(viewModel, new CrystalNavigationEventArgs() { Direction = CrystalNavigationDirection.Backward });

                auxillaryPivotItemFrame.Content = null;
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

                auxillaryPivotItemNavService.NavigateTo<T>(parameter);

                currentViewModel = auxillaryPivotItemNavService.GetNavigatedViewModel();
            }

            pivotControl.SelectionChanged += PivotControl_SelectionChanged;

            navigationLock.Set();
        }

        private void PivotNavigate(Type viewModelType, PivotItem requiredPivot)
        {
            if (currentViewModel != null)
            {
                if (viewModelType == currentViewModel.GetType()) return; //we're already on that page

                viewModelBackStack.Push(currentViewModel);
            }

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
