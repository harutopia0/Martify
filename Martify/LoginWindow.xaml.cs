using Martify.ViewModels;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;

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
                // 1. Tạo môi trường WebView2
                // Lưu cache vào thư mục Temp để tránh lỗi quyền truy cập
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(), "Martify_WebView2"));
                await webView.EnsureCoreWebView2Async(env);

                // 2. Map thư mục WebAssets/YetiAnimatedLogin vào domain ảo
                // Đường dẫn: bin/Debug/net8.0-windows/WebAssets/YetiAnimatedLogin
                string localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebAssets", "YetiAnimatedLogin");

                // Map vào domain: https://martify.login/
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "martify.login",
                    localFolder,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                // 3. Tắt menu chuột phải & DevTools cho gọn
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Làm nền trong suốt để hòa trộn với App
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                // 4. Lắng nghe tin nhắn từ JS gửi về (Username/Pass)
                webView.WebMessageReceived += WebView_WebMessageReceived;

                // 5. Load file index.html
                webView.Source = new Uri("https://martify.login/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải giao diện Login: " + ex.Message);
            }
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Nhận JSON từ script.js: {"username": "...", "password": "..."}
                string jsonString = e.TryGetWebMessageAsString();

                if (string.IsNullOrEmpty(jsonString)) return;

                // Parse JSON
                dynamic data = JsonConvert.DeserializeObject(jsonString);
                string u = data.username;
                string p = data.password;

                // Gán vào ViewModel và gọi lệnh Login
                if (DataContext is LoginVM vm)
                {
                    vm.Username = u;
                    vm.Password = p;

                    if (vm.LoginCommand.CanExecute(this))
                    {
                        vm.LoginCommand.Execute(this);
                    }
                }
            }
            catch { }
        }
    }
}