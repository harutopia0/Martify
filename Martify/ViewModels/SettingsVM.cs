using System.Windows.Input;
using System.Windows; // Nhớ using cái này cho Visibility

namespace Martify.ViewModels
{
    public class SettingsVM : BaseVM
    {
        // 1. Biến kiểm soát ẩn hiện
        private Visibility _configVisibility = Visibility.Collapsed;
        public Visibility ConfigVisibility
        {
            get { return _configVisibility; }
            set { _configVisibility = value; OnPropertyChanged(); }
        }

        // 2. Command để mở và đóng bảng
        public ICommand OpenConfigCommand { get; set; }
        public ICommand CloseConfigCommand { get; set; }

        public SettingsVM()
        {
            // Logic: Bấm mở thì hiện (Visible), bấm đóng thì ẩn (Collapsed)
            OpenConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Visible);
            CloseConfigCommand = new RelayCommand(o => ConfigVisibility = Visibility.Collapsed);
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

                    // GỌI HÀM ĐỔI THEME CỦA APP TẠI ĐÂY
                    var app = Application.Current as App;
                    app?.SetTheme(_isDarkMode);

                    // (Tùy chọn) Lưu lại cài đặt để lần sau mở app vẫn nhớ
                    // Properties.Settings.Default.IsDark = value;
                    // Properties.Settings.Default.Save();
                }
            }
        }

        // Constructor
    }
}
