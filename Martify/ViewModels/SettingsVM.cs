using Martify.Models;
using Martify.Views; // Để gọi EditProfileWindow
using System.Windows;
using System.Windows.Input;
using Martify; // [QUAN TRỌNG 1] Thêm dòng này để nhận diện LoginWindow và App

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

            // Command mở cửa sổ chỉnh sửa
            OpenEditProfileCommand = new RelayCommand(OpenEditProfile);

            LogoutCommand = new RelayCommand(Logout);

            _isDarkMode = _globalDarkModeState;
        }

        void OpenEditProfile(object obj)
        {
            EditProfileWindow editWindow = new EditProfileWindow();
            // [QUAN TRỌNG 2] Phải có dòng này cửa sổ mới hiện lên
            editWindow.ShowDialog();
        }

        private void Logout(object obj)
        {
            DataProvider.Ins.CurrentAccount = null;
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null) return;

            mainWindow.Hide();

            // Cần 'using Martify;' để dùng được LoginWindow
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            if (loginWindow.DataContext is LoginVM loginVM && loginVM.isLogin)
            {
                if (mainWindow.DataContext is MainVM mainVM)
                {
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
                mainWindow.Show();
            }
            else
            {
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

                    _globalDarkModeState = value;

                    // Cần 'using Martify;' để ép kiểu về class App
                    var app = Application.Current as App;
                    app?.SetTheme(_isDarkMode);
                }
            }
        }
    }
}