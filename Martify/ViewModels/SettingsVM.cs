using Martify.Models;
using Martify.Views;
using System.Windows;
using System.Windows.Input;
using Martify;
using System.Windows.Threading;

namespace Martify.ViewModels
{
    public class SettingsVM : BaseVM
    {
        private static bool _globalDarkModeState = false;

        private Visibility _configVisibility = Visibility.Collapsed;
        public Visibility ConfigVisibility
        {
            get => _configVisibility;
            set
            {
                _configVisibility = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenConfigCommand
        {
            get;
            set;
        }
        public ICommand CloseConfigCommand
        {
            get;
            set;
        }
        public ICommand LogoutCommand
        {
            get;
            set;
        }
        public ICommand OpenEditProfileCommand
        {
            get;
            set;
        }

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
            ProductSelectionVM.ClearStaticData();
            DataProvider.Ins.CurrentAccount = null;

            var oldWindow = Application.Current.MainWindow;

            if (oldWindow?.DataContext is MainVM mainVM)
            {
                mainVM.ResetSession();
            }

            oldWindow?.Close();

            LoginWindow loginWindow = new LoginWindow();
            if (loginWindow.DataContext is LoginVM vm)
            {
                vm.isLogin = false;
                vm.Username = "";
                vm.Password = "";
            }
            loginWindow.ShowDialog();

            if (loginWindow.DataContext is LoginVM loginVM && loginVM.isLogin)
            {
                MainWindow newWindow = new MainWindow();
                Application.Current.MainWindow = newWindow;
                newWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
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