using Martify.ViewModels;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks; // Cần thiết cho Task.Delay
using System.Windows;
using System.Windows.Controls; // Cần thiết cho Grid
using System.Windows.Media.Animation;

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

                // 4. Đăng ký sự kiện nhận tin nhắn từ JS
                webView.WebMessageReceived += WebView_WebMessageReceived;

                // 5. Load trang login
                webView.Source = new Uri("https://martify.login/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải giao diện Login: " + ex.Message);
            }
        }

        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Fix lỗi focus khi minimize
                this.Activate();
                this.Focus();

                string jsonString = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(jsonString)) return;

                dynamic data = JsonConvert.DeserializeObject(jsonString);
                string type = data.type;

                if (type == "loaded")
                {
                    // --- XỬ LÝ LOGIC LOADING (2.5 GIÂY) ---

                    // 1. Web đã tải xong. Giữ nguyên màn hình Loading xoay trong 2 giây.
                    await Task.Delay(2000);

                    // 2. Ra lệnh cho JS bắt đầu hiệu ứng mờ dần (Fade Out).
                    // Trong CSS đã set transition: opacity 0.5s.
                    await webView.ExecuteScriptAsync("window.hideLoading();");

                    // 3. Chờ đúng 0.5 giây (500ms) để hiệu ứng mờ hoàn tất.
                    await Task.Delay(500);

                    // --- SAU 2.5s: HIỆN GIAO DIỆN CHÍNH ---

                    // 4. Thu nhỏ WebView về cột trái (Lúc này Loading đã mất hẳn nên không bị giật).
                    Grid.SetColumnSpan(WebViewContainer, 1);

                    // 5. Ra lệnh JS hiện Form Yeti (Fade In).
                    await webView.ExecuteScriptAsync("window.showForm();");

                    // 6. Hiện Panel chữ bên phải (Fade In).
                    DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
                    RightSidePanel.BeginAnimation(OpacityProperty, fadeIn);
                }
                else if (type == "login")
                {
                    string u = data.username;
                    string p = data.password;

                    if (DataContext is LoginVM vm)
                    {
                        vm.Username = u;
                        vm.Password = p;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (vm.LoginCommand.CanExecute(this))
                            {
                                vm.LoginCommand.Execute(this);
                            }
                        });
                    }
                }
            }
            catch { }
        }
    }
}