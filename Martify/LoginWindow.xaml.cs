using Martify.ViewModels;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation; // Cần thiết cho Animation

namespace Martify
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        async void InitializeWebView()
        {
            try
            {
                // 1. Tạo môi trường WebView2 (Cache vào Temp)
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(), "Martify_WebView2"));
                await webView.EnsureCoreWebView2Async(env);

                // 2. Map thư mục WebAssets
                string localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebAssets", "YetiAnimatedLogin");
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "martify.login",
                    localFolder,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                // 3. Cấu hình WebView
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                // 4. Đăng ký sự kiện
                // Sự kiện khi tải xong trang -> Ẩn Loading, Hiện WebViewContainer
                //webView.NavigationCompleted += WebView_NavigationCompleted;

                // Sự kiện nhận thông tin đăng nhập từ JS
                webView.WebMessageReceived += WebView_WebMessageReceived;

                // 5. Load trang login
                webView.Source = new Uri("https://martify.login/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải giao diện Login: " + ex.Message);
            }
        }

        // Xử lý khi WebView tải xong
        //private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        //{
        //    // 1. Ẩn Loading Panel
        //    if (LoadingPanel != null)
        //    {
        //        LoadingPanel.Visibility = Visibility.Collapsed;
        //    }

        //    // 2. Hiện dần WebViewContainer (Thay vì WebView trực tiếp để tránh lỗi Opacity setter)
        //    if (WebViewContainer != null)
        //    {
        //        DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
        //        WebViewContainer.BeginAnimation(OpacityProperty, fadeIn);
        //    }
        //}

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // [QUAN TRỌNG - FIX LỖI MINIMIZE]: 
                // Ép buộc lấy lại Focus từ tiến trình con WebView2
                this.Activate();
                this.Focus();

                string jsonString = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(jsonString)) return;

                dynamic data = JsonConvert.DeserializeObject(jsonString);
                string u = data.username;
                string p = data.password;

                if (DataContext is LoginVM vm)
                {
                    vm.Username = u;
                    vm.Password = p;

                    // Dùng Dispatcher để đảm bảo chạy trên luồng UI chính
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (vm.LoginCommand.CanExecute(this))
                        {
                            vm.LoginCommand.Execute(this);
                        }
                    });
                }
            }
            catch { }
        }
    }
}