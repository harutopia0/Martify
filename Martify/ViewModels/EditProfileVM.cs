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
        private string _username;
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        // [MỚI] Thêm thuộc tính PhoneNumber
        private string _phoneNumber;
        public string PhoneNumber { get => _phoneNumber; set { _phoneNumber = value; OnPropertyChanged(); } }

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

            Username = acc.Username;
            FullName = acc.Employee.FullName;
            Email = acc.Employee.Email;
            PhoneNumber = acc.Employee.Phone; // [MỚI] Load SĐT
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

            // 1. Xử lý lưu ảnh (giữ nguyên logic cũ)
            if (!string.IsNullOrEmpty(AvatarPath) && Path.IsPathRooted(AvatarPath))
            {
                try
                {
                    string extension = Path.GetExtension(AvatarPath);
                    string newFileName = $"{acc.Employee.EmployeeID}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                    string destFolder = Path.Combine(projectDir, "Assets", "Employee");

                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    string destPath = Path.Combine(destFolder, newFileName);
                    File.Copy(AvatarPath, destPath, true);

                    acc.Employee.ImagePath = Path.Combine("Assets", "Employee", newFileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu ảnh: {ex.Message}");
                }
            }

            // 2. Xử lý đổi mật khẩu (giữ nguyên logic cũ)
            var passwordBox = w.FindName("txtNewPass") as PasswordBox;
            var confirmBox = w.FindName("txtConfirmPass") as PasswordBox;
            var oldPassBox = w.FindName("txtOldPass") as PasswordBox;

            if (passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                string inputOldPassHash = LoginVM.ConvertToSHA256(oldPassBox.Password);

                if (inputOldPassHash != acc.HashPassword)
                {
                    MessageBox.Show("Mật khẩu cũ không đúng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (passwordBox.Password != confirmBox.Password)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                acc.HashPassword = LoginVM.ConvertToSHA256(passwordBox.Password);
            }

            // [QUAN TRỌNG] Cập nhật thông tin cá nhân
            // acc.Employee.FullName = FullName; // <-- [ĐÃ BỎ] Không cập nhật tên nữa

            acc.Employee.Email = Email;          // Cập nhật Email
            acc.Employee.Phone = PhoneNumber; // [MỚI] Cập nhật SĐT

            // Lưu xuống Database
            DataProvider.Ins.DB.SaveChanges();

            // Cập nhật lại giao diện chính (SidePanel)
            if (Application.Current.MainWindow.DataContext is MainVM mainVM)
            {
                mainVM.LoadCurrentUserData();
            }

            MessageBox.Show("Cập nhật hồ sơ thành công!", "Thông báo");
            w.Close();
        }
    }
}