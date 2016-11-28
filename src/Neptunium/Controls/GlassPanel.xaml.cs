using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Neptunium.Controls
{
    public sealed partial class GlassPanel : UserControl
    {
        private SpriteVisual glassVisual;
        private Color blurColor = Color.FromArgb(255, 245, 245, 245);

        public GlassPanel()
        {
            this.InitializeComponent();
        }


        private void InitializeFrostedGlass(UIElement glassHost)
        {
            //https://msdn.microsoft.com/en-us/windows/uwp/graphics/using-the-visual-layer-with-xaml

            Visual hostVisual = ElementCompositionPreview.GetElementVisual(glassHost);
            Compositor compositor = hostVisual.Compositor;

            // Create a glass effect, requires Win2D NuGet package
            var glassEffect = new GaussianBlurEffect
            {
                BlurAmount = 10.0f, //original value: 15.0f
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect
                {
                    MultiplyAmount = 0,
                    Source1Amount = 0.5f,
                    Source2Amount = 0.5f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect
                    {
                        Color = blurColor
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

            IsGlassOn = true;
        }

        public bool IsGlassOn { get; private set; }

        public void ChangeBlurColor(Color newColor)
        {
            if (IsGlassOn) TurnOffGlass();

            blurColor = newColor;

            TurnOnGlass();
        }

        public void TurnOnGlass()
        {
            if (IsGlassOn) return;

            InitializeFrostedGlass(GlassHost);

            ElementCompositionPreview.SetElementChildVisual(GlassHost, glassVisual); //turn on glass
        }

        public void TurnOffGlass()
        {
            if (!IsGlassOn) return;

            ElementCompositionPreview.SetElementChildVisual(GlassHost, null); //turn off the glass

            if (glassVisual != null)
            {
                glassVisual.Dispose();
                glassVisual = null;
            }

            IsGlassOn = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (glassVisual != null)
                glassVisual.Dispose();
        }
    }
}
