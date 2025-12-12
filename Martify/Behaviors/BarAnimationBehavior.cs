using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Martify.Behaviors
{
    public static class BarAnimationBehavior
    {
        // Pre-calculated pseudo-random values for optimization (fake random)
        private static readonly double[] PseudoRandomSet1 = { 35, 52, 28, 61, 43, 39, 58 };
        private static readonly double[] PseudoRandomSet2 = { 75, 58, 92, 48, 83, 66, 71 };
        private static readonly double[] PseudoRandomSet3 = { 45, 62, 38, 71, 53, 49, 68 };

        public static readonly DependencyProperty AnimateOnLoadProperty =
            DependencyProperty.RegisterAttached(
                "AnimateOnLoad",
                typeof(bool),
                typeof(BarAnimationBehavior),
                new PropertyMetadata(false, OnAnimateOnLoadChanged));

        public static bool GetAnimateOnLoad(DependencyObject obj)
        {
            return (bool)obj.GetValue(AnimateOnLoadProperty);
        }

        public static void SetAnimateOnLoad(DependencyObject obj, bool value)
        {
            obj.SetValue(AnimateOnLoadProperty, value);
        }

        private static void OnAnimateOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border && (bool)e.NewValue)
            {
                border.Loaded += Border_Loaded;
            }
        }

        private static void Border_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Loaded -= Border_Loaded;
                
                // Start animation sooner - reduced from 850ms to 100ms
                var delay = new System.Windows.Threading.DispatcherTimer();
                delay.Interval = TimeSpan.FromMilliseconds(100 + GetBarIndex(border) * 80);
                delay.Tick += (s, args) =>
                {
                    delay.Stop();
                    AnimateBar(border);
                };
                delay.Start();
            }
        }

        private static int GetBarIndex(Border border)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(border);
            while (parent != null && !(parent is ContentPresenter))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            if (parent is ContentPresenter presenter)
            {
                ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(presenter);
                if (itemsControl != null)
                {
                    return itemsControl.ItemContainerGenerator.IndexFromContainer(presenter);
                }
            }
            return 0;
        }

        private static void AnimateBar(Border border)
        {
            var binding = BindingOperations.GetMultiBindingExpression(border, Border.HeightProperty);
            if (binding == null) return;

            binding.UpdateTarget();
            double finalHeight = border.Height;
            
            if (double.IsNaN(finalHeight) || finalHeight < 2)
                finalHeight = 2;

            var storyboard = new Storyboard();
            
            // Get pseudo-random values based on bar index for consistency and performance
            int barIndex = GetBarIndex(border);
            int index = barIndex % 7; // Cycle through 7 pre-calculated values
            
            var random1 = PseudoRandomSet1[index];
            var random2 = PseudoRandomSet2[index];
            var random3 = PseudoRandomSet3[index];
            
            var animation = new DoubleAnimationUsingKeyFrames();
            
            // Animation timeline - extended duration for longer animation
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            
            // Phase 1: Random bounce 1 (0-350ms)
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                random1, 
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(350)), 
                new CubicEase { EasingMode = EasingMode.EaseOut }
            ));
            
            // Phase 2: Random bounce 2 (350-700ms)
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                random2, 
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(700)), 
                new CubicEase { EasingMode = EasingMode.EaseInOut }
            ));
            
            // Phase 3: Random bounce 3 (700-1050ms)
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                random3, 
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1050)), 
                new CubicEase { EasingMode = EasingMode.EaseInOut }
            ));
            
            // Phase 4: Settle near final (1050-1400ms)
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                finalHeight * 0.95, 
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400)), 
                new CubicEase { EasingMode = EasingMode.EaseOut }
            ));
            
            // Phase 5: Final position with elastic ease (1400-1800ms)
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                finalHeight, 
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1800)), 
                new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 3 }
            ));
            
            Storyboard.SetTarget(animation, border);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Border.HeightProperty));
            storyboard.Children.Add(animation);
            
            // Opacity animation - extended to match new duration
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(1000),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(opacityAnimation, border);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Border.OpacityProperty));
            storyboard.Children.Add(opacityAnimation);
            
            border.Height = 2;
            border.Opacity = 0.3;
            storyboard.Begin();
        }
    }
}

