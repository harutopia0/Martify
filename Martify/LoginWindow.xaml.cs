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
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(), "Martify_WebView2"));
                await webView.EnsureCoreWebView2Async(env);

                string localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebAssets", "YetiAnimatedLogin");

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "martify.login",
                    localFolder,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

                webView.WebMessageReceived += WebView_WebMessageReceived;

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
                // [FIX LỖI MINIMIZE]: 
                // Ép buộc Windows nhận diện ứng dụng WPF đang Active (lấy lại Focus từ WebView2 process)
                // trước khi mở cửa sổ mới.
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

                    // Sử dụng Dispatcher để đảm bảo lệnh mở cửa sổ chạy mượt mà trên luồng UI chính
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