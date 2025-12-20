using Martify.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; 
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
            PhoneNumber = acc.Employee.Phone;
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

        // Hàm kiểm tra định dạng Email
        bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                // Regex đơn giản để check email
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        void SaveChanges(Window w)
        {
            // -----------------------------------------------------
            // [LOGIC KIỂM TRA DỮ LIỆU ĐẦU VÀO]
            // -----------------------------------------------------

            // 1. Kiểm tra Email
            if (string.IsNullOrWhiteSpace(Email))
            {
                MessageBox.Show("Email không được để trống!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!IsValidEmail(Email))
            {
                MessageBox.Show("Định dạng Email không hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kiểm tra Số điện thoại
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                MessageBox.Show("Số điện thoại không được để trống!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Chỉ được chứa số và độ dài từ 9 đến 11 ký tự
            if (!PhoneNumber.All(char.IsDigit))
            {
                MessageBox.Show("Số điện thoại chỉ được chứa ký tự số!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (PhoneNumber.Length < 9 || PhoneNumber.Length > 11)
            {
                MessageBox.Show("Số điện thoại phải từ 9 đến 11 số!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // -----------------------------------------------------

            var acc = DataProvider.Ins.CurrentAccount;
            if (acc == null) return;

            // Xử lý lưu ảnh
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

            // Xử lý đổi mật khẩu
            var passwordBox = w.FindName("txtNewPass") as PasswordBox;
            var confirmBox = w.FindName("txtConfirmPass") as PasswordBox;
            var oldPassBox = w.FindName("txtOldPass") as PasswordBox;

            if (passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password))
            {
                if (string.IsNullOrEmpty(oldPassBox.Password))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu cũ để xác thực!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string inputOldPassHash = LoginVM.ConvertToSHA256(oldPassBox.Password);

                if (inputOldPassHash != acc.HashPassword)
                {
                    MessageBox.Show("Mật khẩu cũ không đúng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (passwordBox.Password.Length < 6)
                {
                    MessageBox.Show("Mật khẩu mới quá ngắn (tối thiểu 6 ký tự)!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (passwordBox.Password != confirmBox.Password)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                acc.HashPassword = LoginVM.ConvertToSHA256(passwordBox.Password);
            }

            // Lưu thông tin hợp lệ
            acc.Employee.Email = Email;
            acc.Employee.Phone = PhoneNumber;

            DataProvider.Ins.DB.SaveChanges();

            if (Application.Current.MainWindow.DataContext is MainVM mainVM)
            {
                mainVM.LoadCurrentUserData();
            }

            MessageBox.Show("Cập nhật hồ sơ thành công!", "Thông báo");
            w.Close();
        }
    }
}