using System;
using System.IO;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Martify.ViewModels; // Nhớ using namespace chứa SettingsVM
using System.Windows;

namespace Martify.Views
{
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            InitializeWebView();

            this.Unloaded += (s, e) =>
            {
                ThemeSwitchWebView.Visibility = Visibility.Collapsed;
                ThemeSwitchWebView.Dispose(); // Tùy chọn: Giải phóng tài nguyên nếu cần
            };

            // Khi quay lại tab này, hiện lại WebView
            this.Loaded += (s, e) =>
            {
                ThemeSwitchWebView.Visibility = Visibility.Visible;
            };
        }

        async void InitializeWebView()
        {
            await ThemeSwitchWebView.EnsureCoreWebView2Async(null);

            // Lưu ý: Trong ảnh bạn gửi tên file là "indext.html" (thừa chữ t), 
            // hãy chắc chắn bạn đã đổi tên thành "index.html" hoặc sửa code cho khớp tên file.
            string assetsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebAssets", "ThemeToggle");

            ThemeSwitchWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "martify.toggle",
                assetsFolderPath,
                CoreWebView2HostResourceAccessKind.Allow
            );

            ThemeSwitchWebView.WebMessageReceived += ThemeSwitchWebView_WebMessageReceived;
            ThemeSwitchWebView.Source = new Uri("https://martify.toggle/index.html");
        }

        // 1. Đồng bộ trạng thái từ VM -> WebView (Khi vừa load xong)
        private void ThemeSwitchWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Lấy ViewModel từ DataContext
            if (DataContext is SettingsVM vm)
            {
                // Gọi hàm JS setSwitchState dựa trên biến IsDarkMode của VM
                string script = $"setSwitchState({vm.IsDarkMode.ToString().ToLower()});";
                ThemeSwitchWebView.ExecuteScriptAsync(script);
            }
        }

        // 2. Nhận lệnh từ WebView -> Cập nhật VM (Khi người dùng bấm nút)
        private void ThemeSwitchWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            // Lấy ViewModel từ DataContext
            if (DataContext is SettingsVM vm)
            {
                if (message == "DarkMode")
                {
                    vm.IsDarkMode = true; // Setter của VM sẽ tự gọi App.SetTheme
                }
                else
                {
                    vm.IsDarkMode = false;
                }
            }
        }
    }
}