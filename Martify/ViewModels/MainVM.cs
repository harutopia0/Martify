using Martify.Models;
using Martify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class MainVM : BaseVM
    {
        public bool isLoaded = false;
        public ICommand LoadedWindowCommand { get; set; }
        public NavigationVM Navigation { get; }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        // [SỬA LỖI] Đặt mặc định là -1. 
        // Điều này đảm bảo khi App mới chạy, chưa có nút nào được chọn.
        // Khi đăng nhập xong, code sẽ set thẳng vào 0 hoặc 1, tránh việc chuyển từ 0->1 gây hiệu ứng tắt.
        private int _selectedMenuIndex = -1;
        public int SelectedMenuIndex
        {
            get { return _selectedMenuIndex; }
            set { _selectedMenuIndex = value; OnPropertyChanged(); }
        }

        private string _FullName;
        public string FullName
        {
            get { return _FullName; }
            set { _FullName = value; OnPropertyChanged(); }
        }

        private string _Email;
        public string Email
        {
            get { return _Email; }
            set { _Email = value; OnPropertyChanged(); }
        }

        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged(); }
        }


        public MainVM()
        {
            Navigation = new NavigationVM();

            LoadedWindowCommand = new RelayCommand<Window>((p) => { return true; }, (p) => {
                isLoaded = true;
                if (p == null) return;
                p.Hide();
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.ShowDialog();

                if (loginWindow.DataContext == null) return;
                var loginVM = loginWindow.DataContext as LoginVM;

                if (loginVM.isLogin)
                {
                    LoadCurrentUserData();
                    p.Show();
                }
                else p.Close();
            });
        }

        public void LoadCurrentUserData()
        {
            var acc = DataProvider.Ins.CurrentAccount;

            if (acc != null && acc.Employee != null)
            {
                FullName = acc.Employee.FullName;
                Email = acc.Employee.Email;
                ImagePath = acc.Employee.ImagePath;

                if (acc.Role == 0) // Admin
                {
                    IsAdmin = true;
                    Navigation.DashboardCommand.Execute(null);
                    SelectedMenuIndex = 0; // Chọn Dashboard
                }
                else // Staff
                {
                    IsAdmin = false;
                    Navigation.ProductSelectionCommand.Execute(null);
                    SelectedMenuIndex = 1; // Chọn Sell -> Sell sẽ sáng lên ngay lập tức, Dashboard vẫn giữ nguyên trạng thái tắt
                }
            }
            else
            {
                FullName = "N/A";
                Email = "Chưa cập nhật";
                ImagePath = null;
                IsAdmin = false;
                SelectedMenuIndex = -1;
            }
        }
    }
}