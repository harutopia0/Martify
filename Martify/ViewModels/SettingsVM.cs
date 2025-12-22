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
            // 1. Dọn dẹp dữ liệu tĩnh & Session
            ProductSelectionVM.ClearStaticData();
            DataProvider.Ins.CurrentAccount = null;
            ConfigVisibility = Visibility.Collapsed;

            // 2. Lấy cửa sổ hiện tại (cái cũ)
            var oldWindow = Application.Current.MainWindow;

            // 3. Tạo cửa sổ mới
            // KHI CỬA SỔ NÀY ĐƯỢC "SHOW", NÓ SẼ TỰ ĐỘNG CHẠY LoadedWindowCommand CỦA BẠN
            // VÀ TỰ ĐỘNG HIỆN LOGIN WINDOW.
            MainWindow newWindow = new MainWindow();

            // 4. Gán cửa sổ chính của App sang cái mới (để tránh tắt App khi đóng cái cũ)
            Application.Current.MainWindow = newWindow;

            // 5. Hiển thị cửa sổ mới
            // (Ngay lúc này, MainVM sẽ chặn lại, ẩn đi và hiện Login như bạn muốn)
            newWindow.Show();

            // 6. Đóng cửa sổ cũ đi
            oldWindow?.Close();
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