using Crystal3.Navigation;
using Crystal3.UI;
using Crystal3.Utilities;
using Microsoft.Graphics.Canvas.Effects;
using Neptunium.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Neptunium.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Crystal3.Navigation.NavigationViewModel(typeof(NowPlayingViewViewModel), NavigationViewSupportedPlatform.Desktop | NavigationViewSupportedPlatform.Mobile)]
    public sealed partial class NowPlayingView : Page
    {
        private SpriteVisual glassVisual;
        private NowPlayingViewViewModel viewModel = null;

        public NowPlayingView()
        {
            this.InitializeComponent();
            InitializeFrostedGlass(GlassHost);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ElementCompositionPreview.SetElementChildVisual(GlassHost, null); //turn off the glass
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NowPlayingBackgroundImage")
            {
                var viewModel = (this.DataContext as NowPlayingViewViewModel);

                await Task.Delay(500);

                var imgSrc = viewModel.NowPlayingBackgroundImage;

                if (string.IsNullOrWhiteSpace(imgSrc))
                {
                    ElementCompositionPreview.SetElementChildVisual(GlassHost, null); //turn off the glass
                }
                else
                {
                    //todo check if url doesn't 404

                    ElementCompositionPreview.SetElementChildVisual(GlassHost, glassVisual); //turn on glass
                }
            }
        }

        private void InitializeFrostedGlass(UIElement glassHost)
        {
            //https://msdn.microsoft.com/en-us/windows/uwp/graphics/using-the-visual-layer-with-xaml

            Visual hostVisual = ElementCompositionPreview.GetElementVisual(glassHost);
            Compositor compositor = hostVisual.Compositor;

            // Create a glass effect, requires Win2D NuGet package
            var glassEffect = new GaussianBlurEffect
            {
                BlurAmount = 7.0f, //original value: 15.0f
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect
                {
                    MultiplyAmount = 0,
                    Source1Amount = 0.5f,
                    Source2Amount = 0.5f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect
                    {
                        Color = Color.FromArgb(255, 245, 245, 245)
                    }
                }
            };

            //  Create an instance of the effect and set its source to a CompositionBackdropBrush
            var effectFactory = compositor.CreateEffectFactory(glassEffect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            // Create a Visual to contain the frosted glass effect
            glassVisual = compositor.CreateSpriteVisual();
            glassVisual.Brush = effectBrush;

            // Add the blur as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(glassHost, glassVisual);

            // Make sure size of glass host and glass visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

            glassVisual.StartAnimation("Size", bindSizeAnimation);
        }

        private void Page_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            viewModel = (args.NewValue as NowPlayingViewViewModel);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            glassVisual.Dispose();
        }
    }
}
