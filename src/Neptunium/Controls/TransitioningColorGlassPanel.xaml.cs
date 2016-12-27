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
    public sealed partial class TransitioningColorGlassPanel : UserControl
    {
        private SpriteVisual glassVisual;
        private Color blurColor = Color.FromArgb(255, 245, 245, 245);
        private Color lastBlurColor = Colors.Transparent;
        private DispatcherTimer timer = new DispatcherTimer();
        private Color[] colorsLoop = new Color[] { Colors.Blue, Colors.Purple, Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Teal };
        private int colorsLoopIndex = 0;

        public TransitioningColorGlassPanel()
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
                Name = "Blur",
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
                        Name = "NewColor",
                        Color = blurColor
                    }
                }
            };

            //  Create an instance of the effect and set its source to a CompositionBackdropBrush
            var effectFactory = compositor.CreateEffectFactory(glassEffect, new[] { "Blur.BlurAmount", "NewColor.Color" });
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            // Create a Visual to contain the frosted glass effect
            glassVisual = compositor.CreateSpriteVisual();
            glassVisual.Brush = effectBrush;

            if (Animate)
            {
                // https://blogs.windows.com/buildingapps/2016/09/12/creating-beautiful-effects-for-uwp/

                ColorKeyFrameAnimation colorAnimation = compositor.CreateColorKeyFrameAnimation();
                colorAnimation.InsertKeyFrame(0.0f, lastBlurColor);
                colorAnimation.InsertKeyFrame(1.0f, blurColor);
                colorAnimation.Duration = TimeSpan.FromSeconds(5);
                effectBrush.StartAnimation("NewColor.Color", colorAnimation);

                //ScalarKeyFrameAnimation blurAnimation = compositor.CreateScalarKeyFrameAnimation();
                //blurAnimation.InsertKeyFrame(0.0f, 0.0f);
                //blurAnimation.InsertKeyFrame(0.5f, 100.0f);
                //blurAnimation.InsertKeyFrame(1.0f, 0.0f);
                //blurAnimation.Duration = TimeSpan.FromSeconds(4);
                //blurAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
                //effectBrush.StartAnimation("Blur.BlurAmount", blurAnimation);
            }

            // Add the blur as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(glassHost, glassVisual);

            // Make sure size of glass host and glass visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

            glassVisual.StartAnimation("Size", bindSizeAnimation);

            IsGlassOn = true;
        }

        public void StartAnimating()
        {
            if (!timer.IsEnabled)
                timer.Start();
        }
        public void StopAnimating()
        {
            if (timer.IsEnabled)
                timer.Stop();
        }

        private bool Animate { get { return true; } }

        private bool IsGlassOn { get; set; }

        private void ChangeBlurColor(Color newColor)
        {
            if (IsGlassOn) TurnOffGlass();

            lastBlurColor = blurColor;
            blurColor = newColor;

            TurnOnGlass();
        }

        private void TurnOnGlass()
        {
            if (IsGlassOn) return;

            InitializeFrostedGlass(GlassHost);

            ElementCompositionPreview.SetElementChildVisual(GlassHost, glassVisual); //turn on glass
        }

        private void TurnOffGlass()
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
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 5);
        }

        private void Timer_Tick(object sender, object e)
        {
            if (colorsLoopIndex >= colorsLoop.Length)
                colorsLoopIndex = 0;

            var color = colorsLoop[colorsLoopIndex];
            ChangeBlurColor(color);

            colorsLoopIndex++;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimating();

            timer.Tick -= Timer_Tick;

            if (glassVisual != null)
                glassVisual.Dispose();
        }
    }
}
