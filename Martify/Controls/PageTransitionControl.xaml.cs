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
        private bool _isTransitioning = false;
        private object _pendingPage = null;

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
                // Nếu đang chuyển trang, chỉ lưu lại trang mới nhất và thoát
                if (control._isTransitioning)
                {
                    control._pendingPage = e.NewValue;
                    return;
                }

                await control.InternalTransition(e.NewValue);
            }
        }

        public PageTransitionControl()
        {
            InitializeComponent();
        }

        private async Task InternalTransition(object newPage)
        {
            if (newPage == null) return;

            _isTransitioning = true;
            _pendingPage = null; // Xóa trang chờ vì chúng ta đang xử lý trang hiện tại

            try
            {
                // Bước 1: Fade out
                await FadeOutContent();

                // Bước 3: Cập nhật nội dung
                PageContent.Content = newPage;

                // Bước 2: Hiển thị loading và đợi
                LoadingOverlay.Visibility = Visibility.Visible;
                await Task.Delay(500);



                // Bước 4: Ẩn loading
                LoadingOverlay.Visibility = Visibility.Collapsed;

                // Bước 5: Fade in
                await FadeInContent();
            }
            finally
            {
                _isTransitioning = false;

                // KIỂM TRA: Nếu trong lúc đang chạy có một click mới (_pendingPage có dữ liệu)
                // thì thực hiện chuyển đến trang đó ngay lập tức.
                if (_pendingPage != null)
                {
                    await InternalTransition(_pendingPage);
                }
            }
        }

        private Task FadeOutContent()
        {
            var tcs = new TaskCompletionSource<bool>();
            var storyboard = new Storyboard();

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