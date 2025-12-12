using Martify.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class EditProfileVM : BaseVM
    {
        // Các biến lưu thông tin
        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _avatarPath;
        public string AvatarPath { get => _avatarPath; set { _avatarPath = value; OnPropertyChanged(); } }

        public ICommand UploadImageCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        public EditProfileVM()
        {
            LoadCurrentData();

            UploadImageCommand = new RelayCommand(UploadImage);
            CloseCommand = new RelayCommand<Window>((w) => w != null, (w) => w.Close());
            SaveCommand = new RelayCommand<Window>((w) => true, (w) => SaveChanges(w));
        }

        void LoadCurrentData()
        {
            var acc = DataProvider.Ins.CurrentAccount;
            if (acc == null || acc.Employee == null) return;

            FullName = acc.Employee.FullName;
            Email = acc.Employee.Email;
            AvatarPath = acc.Employee.ImagePath;
        }

        void UploadImage(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            if (openFileDialog.ShowDialog() == true)
            {
                AvatarPath = openFileDialog.FileName;
            }
        }

        void SaveChanges(Window w)
        {
            var acc = DataProvider.Ins.CurrentAccount;
            if (acc == null) return;

            // 1. Cập nhật thông tin cơ bản
            acc.Employee.FullName = FullName;
            acc.Employee.Email = Email;

            // 2. Xử lý lưu ảnh
            if (!string.IsNullOrEmpty(AvatarPath) && AvatarPath != acc.Employee.ImagePath)
            {
                acc.Employee.ImagePath = AvatarPath;
            }

            // 3. Xử lý đổi mật khẩu
            var passwordBox = w.FindName("txtNewPass") as PasswordBox;
            var confirmBox = w.FindName("txtConfirmPass") as PasswordBox;
            var oldPassBox = w.FindName("txtOldPass") as PasswordBox;

            if (passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                // So sánh với HashPassword trong Database
                if (oldPassBox.Password != acc.HashPassword)
                {
                    MessageBox.Show("Mật khẩu cũ không đúng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (passwordBox.Password != confirmBox.Password)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Cập nhật HashPassword mới
                acc.HashPassword = passwordBox.Password;
            }

            // 4. Lưu xuống DB
            DataProvider.Ins.DB.SaveChanges();

            // 5. Cập nhật lại giao diện chính (LoadCurrentUserData phải là Public)
            var mainVM = Application.Current.MainWindow.DataContext as MainVM;
            if (mainVM != null)
            {
                mainVM.LoadCurrentUserData();
            }

            MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo");
            w.Close();
        }
    }
}