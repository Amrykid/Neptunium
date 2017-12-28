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
        private bool unloaded = false;
        private SpriteVisual glassVisual;
        private Color blurColor = Color.FromArgb(255, 245, 245, 245);
        private Color lastBlurColor = Colors.Transparent;
        private Visual hostVisual = null;
        private Compositor compositor = null;
        private CompositionBackdropBrush backdropBrush = null;
        private CompositionEffectFactory effectFactory = null;
        private CompositionEffectBrush effectBrush = null;
        private GaussianBlurEffect glassEffect = null;

        public GlassPanel()
        {
            this.InitializeComponent();

            hostVisual = ElementCompositionPreview.GetElementVisual(GlassHost);
            compositor = hostVisual.Compositor;

            // Create a Visual to contain the frosted glass effect
            glassVisual = compositor.CreateSpriteVisual();

            backdropBrush = compositor.CreateBackdropBrush();
        }


        private void InitializeFrostedGlass(UIElement glassHost)
        {
            //https://msdn.microsoft.com/en-us/windows/uwp/graphics/using-the-visual-layer-with-xaml

            CleanUp();

            // Create a glass effect, requires Win2D NuGet package
            glassEffect = new GaussianBlurEffect
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
            effectFactory = compositor.CreateEffectFactory(glassEffect, new[] { "Blur.BlurAmount", "NewColor.Color" });
            effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            glassVisual.Brush = effectBrush;

            if (Animate)
            {
                // https://blogs.windows.com/buildingapps/2016/09/12/creating-beautiful-effects-for-uwp/

                ColorKeyFrameAnimation colorAnimation = compositor.CreateColorKeyFrameAnimation();
                colorAnimation.InsertKeyFrame(0.0f, lastBlurColor);
                colorAnimation.InsertKeyFrame(1.0f, blurColor);
                colorAnimation.Duration = TimeSpan.FromSeconds(2);
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

            SetValue(IsGlassOnProperty, true);
        }

        private void CleanUp()
        {
            effectBrush?.Dispose();
            effectFactory?.Dispose();

            glassEffect?.Dispose();
        }

        public bool Animate { get; set; } = true;

        public bool IsGlassOn
        {
            get { return (bool)GetValue(IsGlassOnProperty); }
            set { SetValue(IsGlassOnProperty, value); }
        }

        public static readonly DependencyProperty IsGlassOnProperty = DependencyProperty.Register(nameof(IsGlassOn), typeof(bool), typeof(GlassPanel), new PropertyMetadata(false, new PropertyChangedCallback(OnIsGlassOnChanged)));

        private static void OnIsGlassOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(typeof(GlassPanel).Name + " - " + nameof(OnIsGlassOnChanged) + ": " + e.NewValue);
#endif

            if (((GlassPanel)d).unloaded) return;

            if ((bool)e.NewValue == true)
            {
                ((GlassPanel)d).TurnOnGlassInternal();
            }
            else
            {
                ((GlassPanel)d).TurnOffGlassInternal();
            }
        }

        public void ChangeBlurColor(Color newColor)
        {
            if (IsGlassOn) TurnOffGlass();

            lastBlurColor = blurColor;
            blurColor = newColor;

            TurnOnGlass();
        }

        public void SetBlurColor(Color newColor)
        {
            lastBlurColor = blurColor;
            blurColor = newColor;
        }

        public void TurnOnGlass()
        {
            if (IsGlassOn) return;

            TurnOnGlassInternal();
        }

        private void TurnOnGlassInternal()
        {
            InitializeFrostedGlass(GlassHost);

            ElementCompositionPreview.SetElementChildVisual(GlassHost, glassVisual); //turn on glass
        }

        public void TurnOffGlass()
        {
            if (!IsGlassOn) return;

            TurnOffGlassInternal();
        }

        private async void TurnOffGlassInternal()
        {
            if (glassVisual != null)
            {
                CleanUp();

                // Create a glass effect, requires Win2D NuGet package
                glassEffect = new GaussianBlurEffect
                {
                    Name = "Blur",
                    BlurAmount = 0.0f,
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
                effectFactory = compositor.CreateEffectFactory(glassEffect, new[] { "Blur.BlurAmount", "NewColor.Color" });
                effectBrush = effectFactory.CreateBrush();

                effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

                // Create a Visual to contain the frosted glass effect
                glassVisual = compositor.CreateSpriteVisual();
                glassVisual.Brush = effectBrush;

                if (Animate)
                {
                    // https://blogs.windows.com/buildingapps/2016/09/12/creating-beautiful-effects-for-uwp/

                    ColorKeyFrameAnimation colorAnimation = compositor.CreateColorKeyFrameAnimation();
                    colorAnimation.InsertKeyFrame(0.0f, blurColor);
                    colorAnimation.InsertKeyFrame(1.0f, Colors.Transparent);
                    colorAnimation.Duration = TimeSpan.FromSeconds(2);
                    effectBrush.StartAnimation("NewColor.Color", colorAnimation);
                }

                // Make sure size of glass host and glass visual always stay in sync
                var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
                bindSizeAnimation.SetReferenceParameter("hostVisual", hostVisual);

                glassVisual.StartAnimation("Size", bindSizeAnimation);

                // Add the blur as a child of the host in the visual tree
                ElementCompositionPreview.SetElementChildVisual(GlassHost, glassVisual);

                //glassVisual.Dispose();
                //glassVisual = null;
            }

            SetValue(IsGlassOnProperty, false);

            //ElementCompositionPreview.SetElementChildVisual(GlassHost, null); //turn off the glass
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();

            if (glassVisual != null)
                glassVisual.Dispose();

            unloaded = true;
        }
    }
}
