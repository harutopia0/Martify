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
    }
}