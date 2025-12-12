using Martify.Models;
using Martify.Views;
using System.Windows;
using System.Windows.Input;

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

        public SettingsVM()
        {
            OpenConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Visible);
            CloseConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Collapsed);
            LogoutCommand = new RelayCommand(Logout);

            
            _isDarkMode = _globalDarkModeState;
        }

        private void Logout(object obj)
        {
            // Logic đăng xuất giữ nguyên như cũ
            DataProvider.Ins.CurrentAccount = null;
            var mainWindow = Application.Current.MainWindow;
            mainWindow.Hide();
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

                    // Gọi hàm đổi theme của App
                    var app = Application.Current as App;
                    app?.SetTheme(_isDarkMode);
                }
            }
        }
    }
}