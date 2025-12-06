using Martify.Models; // Cần để truy cập DataProvider, Account
using Martify.Views;  // (Tùy chọn) Nếu LoginWindow nằm trong Views
using System.Windows; // Cần cho Application, Visibility, Window
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class SettingsVM : BaseVM
    {
        // 1. Biến kiểm soát ẩn hiện Popup Cấu hình
        private Visibility _configVisibility = Visibility.Collapsed;
        public Visibility ConfigVisibility
        {
            get { return _configVisibility; }
            set { _configVisibility = value; OnPropertyChanged(); }
        }

        // 2. Các Command
        public ICommand OpenConfigCommand { get; set; }
        public ICommand CloseConfigCommand { get; set; }
        public ICommand LogoutCommand { get; set; } // Command cho nút Đăng xuất

        public SettingsVM()
        {
            // Logic: Bấm mở thì hiện (Visible), bấm đóng thì ẩn (Collapsed)
            OpenConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Visible);
            CloseConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Collapsed);

            // Logic Đăng xuất
            LogoutCommand = new RelayCommand(Logout);
        }

        private void Logout(object obj)
        {
            // B1: Xóa thông tin tài khoản hiện tại
            DataProvider.Ins.CurrentAccount = null;

            // B2: Lấy cửa sổ chính (MainWindow) hiện tại
            var mainWindow = Application.Current.MainWindow;

            // B3: Ẩn MainWindow đi (để tạo cảm giác đã thoát)
            mainWindow.Hide();

            // B4: Hiện lại LoginWindow dưới dạng Dialog (chặn tương tác cho đến khi xong)
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            // B5: Kiểm tra kết quả sau khi LoginWindow đóng lại
            if (loginWindow.DataContext is LoginVM loginVM && loginVM.isLogin)
            {
                // Nếu đăng nhập lại thành công -> Cập nhật lại dữ liệu hiển thị trên MainVM
                // Vì MainVM đang là DataContext của MainWindow
                if (mainWindow.DataContext is MainVM mainVM)
                {
                    // Logic cập nhật thông tin user (giống hàm LoadCurrentUserData trong MainVM)
                    var acc = DataProvider.Ins.CurrentAccount;
                    if (acc != null && acc.Employee != null)
                    {
                        mainVM.FullName = acc.Employee.FullName;
                        mainVM.Email = acc.Employee.Email;
                        mainVM.ImagePath = acc.Employee.ImagePath;
                    }
                    else
                    {
                        mainVM.FullName = "N/A";
                        mainVM.Email = "Chưa cập nhật";
                        mainVM.ImagePath = null;
                    }
                }

                // Hiện lại MainWindow
                mainWindow.Show();
            }
            else
            {
                // Nếu người dùng tắt LoginWindow mà không đăng nhập -> Đóng luôn app
                mainWindow.Close();
            }
        }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();

                    // Gọi hàm đổi theme của App
                    var app = Application.Current as App;
                    app?.SetTheme(_isDarkMode);
                }
            }
        }
    }
}