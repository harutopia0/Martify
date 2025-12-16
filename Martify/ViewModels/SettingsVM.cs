using Martify.Models;
using Martify.Views;
using System.Windows;
using System.Windows.Input;
using Martify;

namespace Martify.ViewModels
{
    public class SettingsVM : BaseVM
    {
       
        private static bool _globalDarkModeState = false;

        private Visibility _configVisibility = Visibility.Collapsed;
        public Visibility ConfigVisibility
        {
            get { return _configVisibility; }
            set { _configVisibility = value; OnPropertyChanged(); }
        }

        public ICommand OpenConfigCommand { get; set; }
        public ICommand CloseConfigCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand OpenEditProfileCommand { get; set; }

        public SettingsVM()
        {
            OpenConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Visible);
            CloseConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Collapsed);
            OpenEditProfileCommand = new RelayCommand(OpenEditProfile);
            LogoutCommand = new RelayCommand(Logout);

            _isDarkMode = _globalDarkModeState;
        }

        void OpenEditProfile(object obj)
        {
            EditProfileWindow editWindow = new EditProfileWindow();
            editWindow.ShowDialog();
        }

        private void Logout(object obj)
        {
            // [MỚI] Xóa sạch các đơn hàng đang treo khi đăng xuất
            ProductSelectionVM.ClearStaticData();

            DataProvider.Ins.CurrentAccount = null;
            ConfigVisibility = Visibility.Collapsed;

         
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
            {
                foreach (Window w in Application.Current.Windows)
                {
                    if (w.DataContext is MainVM) { mainWindow = w; break; }
                }
            }
            if (mainWindow != null) mainWindow.Hide();

           
            LoginWindow loginWindow = new LoginWindow();

            if (loginWindow.DataContext is LoginVM vm)
            {
                vm.isLogin = false;
                vm.Username = ""; 
                vm.Password = ""; 
            }

            // 4. Hiện màn hình đăng nhập
            loginWindow.ShowDialog();

            // 5. Kiểm tra kết quả sau khi đóng form Login
            if (loginWindow.DataContext is LoginVM loginVM && loginVM.isLogin)
            {
                // Chỉ khi đăng nhập thành công (isLogin = true) mới hiện lại Main
                if (mainWindow != null && mainWindow.DataContext is MainVM mainVM)
                {
                    mainVM.LoadCurrentUserData();
                }
                mainWindow?.Show();
            }
            else
            {
                // Nếu tắt form Login (isLogin = false) -> Tắt App
                System.Environment.Exit(0);
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
                    _globalDarkModeState = value;
                    var app = Application.Current as App;
                    app?.SetTheme(_isDarkMode);
                }
            }
        }
    }
}