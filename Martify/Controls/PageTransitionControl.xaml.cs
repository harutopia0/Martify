using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Martify.Controls
{
    public partial class PageTransitionControl : UserControl
    {
        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(
                nameof(CurrentPage),
                typeof(object),
                typeof(PageTransitionControl),
                new PropertyMetadata(null, OnCurrentPageChanged));

        public object CurrentPage
        {
            get => GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        private static async void OnCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PageTransitionControl control)
            {
                await control.TransitionToNewPage(e.NewValue);
            }
        }

        public PageTransitionControl()
        {
            InitializeComponent();
        }

        private async Task TransitionToNewPage(object newPage)
        {
            if (newPage == null) return;

            // Step 1: Fade out current content
            await FadeOutContent();

            // Step 2: Show loading overlay
            LoadingOverlay.Visibility = Visibility.Visible;
            await Task.Delay(600); // Simulate loading time

            // Step 3: Update content
            PageContent.Content = newPage;

            // Step 4: Hide loading overlay
            LoadingOverlay.Visibility = Visibility.Collapsed;

            // Step 5: Fade in new content
            await FadeInContent();
        }

        private Task FadeOutContent()
        {
            var tcs = new TaskCompletionSource<bool>();

            var storyboard = new Storyboard();

            // Fade out opacity
            var fadeOut = new DoubleAnimation
            {
                From = PageContent.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, PageContent);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            // Slide up slightly
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -20,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(slideUp, ContentTranslate);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideUp);

            storyboard.Completed += (s, e) => tcs.SetResult(true);
            storyboard.Begin();

            return tcs.Task;
        }

        private Task FadeInContent()
        {
            var tcs = new TaskCompletionSource<bool>();

            var storyboard = new Storyboard();

            // Fade in opacity
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, PageContent);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            // Slide in from below
            var slideIn = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideIn, ContentTranslate);
            Storyboard.SetTargetProperty(slideIn, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Children.Add(slideIn);

            storyboard.Completed += (s, e) => tcs.SetResult(true);
            storyboard.Begin();

            return tcs.Task;
        }
    }
}