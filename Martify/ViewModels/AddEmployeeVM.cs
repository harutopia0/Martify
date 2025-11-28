using Martify.Models;
using Martify.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class AddEmployeeVM : BaseVM
    {
        // --- Properties Binding ---
        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _address;
        public string Address { get => _address; set { _address = value; OnPropertyChanged(); } }

        private string _phone;
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _gender;
        public string Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }

        private DateTime? _birthDate;
        public DateTime? BirthDate { get => _birthDate; set { _birthDate = value; OnPropertyChanged(); } }

        private DateTime? _hireDate;
        public DateTime? HireDate { get => _hireDate; set { _hireDate = value; OnPropertyChanged(); } }

        // Biến hiển thị ảnh trên View
        private string _selectedImagePath;
        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set { _selectedImagePath = value; OnPropertyChanged(); }
        }

        // Biến lưu đường dẫn ảnh gốc tạm thời
        private string _sourceImageFile;

        // --- Commands ---
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand GenderSelectionChangedCommand { get; set; }
        public ICommand DragWindowCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }

        public AddEmployeeVM()
        {
            // 1. Lệnh Đóng window
            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) => p?.Close());

            // 2. Lệnh Lưu nhân viên
            SaveCommand = new RelayCommand<Window>((p) => { return true; }, (p) => SaveEmployee(p));

            // 3. Xử lý chọn giới tính
            GenderSelectionChangedCommand = new RelayCommand<ListBox>((p) => { return p != null; }, (p) =>
            {
                if (p.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag != null)
                {
                    Gender = selectedItem.Tag.ToString();
                }
            });

            // 4. Kéo Window
            DragWindowCommand = new RelayCommand<Window>((p) => p != null, (p) => { try { p.DragMove(); } catch { } });

            // 5. Chọn Ảnh
            SelectImageCommand = new RelayCommand<object>((p) => true, (p) => SelectImage());
        }

        // --- Hàm Chọn Ảnh ---
        void SelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                _sourceImageFile = openFileDialog.FileName;
                SelectedImagePath = _sourceImageFile;
            }
        }

        // --- Hàm Lưu Nhân Viên ---
        void SaveEmployee(Window p)
        {
            if (!ValidateInput()) return;

            string newEmpId = GenerateEmployeeID();
            string dbPath = null;

            // --- BƯỚC 1: XỬ LÝ COPY ẢNH VÀO Assets/Employee/ ---
            if (!string.IsNullOrEmpty(_sourceImageFile))
            {
                try
                {
                    string fileExt = Path.GetExtension(_sourceImageFile);
                    string fileName = $"{newEmpId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";

                    // A. Copy vào thư mục BIN/Assets/Employee
                    string binFolder = AppDomain.CurrentDomain.BaseDirectory;
                    // SỬA: Thêm "Employee" vào đường dẫn
                    string binAssetsPath = Path.Combine(binFolder, "Assets", "Employee");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(binAssetsPath)) Directory.CreateDirectory(binAssetsPath);

                    string destBinFile = Path.Combine(binAssetsPath, fileName);
                    File.Copy(_sourceImageFile, destBinFile, true);

                    // B. Copy vào thư mục SOURCE CODE/Assets/Employee (Để lưu trữ lâu dài)
                    try
                    {
                        string projectFolder = Path.GetFullPath(Path.Combine(binFolder, @"..\..\..\"));
                        // SỬA: Thêm "Employee" vào đường dẫn
                        string sourceEmployeeAssetsPath = Path.Combine(projectFolder, "Assets", "Employee");

                        // Tạo thư mục nếu chưa có
                        if (!Directory.Exists(sourceEmployeeAssetsPath)) Directory.CreateDirectory(sourceEmployeeAssetsPath);

                        string destSourceFile = Path.Combine(sourceEmployeeAssetsPath, fileName);
                        File.Copy(_sourceImageFile, destSourceFile, true);
                    }
                    catch { /* Bỏ qua lỗi nếu không tìm thấy source code (máy client) */ }

                    // C. Gán đường dẫn tương đối để lưu DB: "Assets/Employee/TenFile.jpg"
                    dbPath = Path.Combine("Assets", "Employee", fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // --- BƯỚC 2: TẠO DATA & LƯU DB ---

            var newEmployee = new Models.Employee()
            {
                EmployeeID = newEmpId,
                FullName = FullName,
                Address = Address,
                Phone = Phone,
                Email = Email,
                Status = true,
                Gender = Gender,
                BirthDate = BirthDate.Value,
                HireDate = HireDate.Value,


                ImagePath = dbPath // Lưu đường dẫn Assets/Employee/...
            };

            // Tạo Account tự động
            string firstName = GetFirstName(FullName);
            string username = (ConvertToUnSign(firstName) + newEmpId).ToLower().Replace(" ", "");
            string rawPassword = BirthDate.Value.ToString("ddMMyyyy");
            string hashPassword = CalculateSHA256(rawPassword);

            var newAccount = new Account()
            {
                Username = username,
                HashPassword = hashPassword,
                Role = 1,
                EmployeeID = newEmpId
            };

            newEmployee.Accounts = new List<Account> { newAccount };

            try
            {
                DataProvider.Ins.DB.Employees.Add(newEmployee);
                DataProvider.Ins.DB.SaveChanges();

                MessageBox.Show($"Thêm thành công!\n\n- Mã NV: {newEmpId}\n- Tài khoản: {username}\n- Mật khẩu: {rawPassword}",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                p?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message + "\n" + ex.InnerException?.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Validate ---
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FullName)) return ShowError("Vui lòng nhập họ và tên.");
            if (string.IsNullOrWhiteSpace(Address)) return ShowError("Vui lòng nhập nơi cư trú.");

            if (string.IsNullOrEmpty(Phone) || !Regex.IsMatch(Phone, @"^[0-9]+$")) return ShowError("Số điện thoại chỉ được chứa số.");
            if (Phone.Length < 9 || Phone.Length > 12) return ShowError("Số điện thoại phải từ 9-12 số.");
            if (DataProvider.Ins.DB.Employees.Any(x => x.Phone == Phone)) return ShowError("Số điện thoại này đã tồn tại.");

            if (string.IsNullOrWhiteSpace(Email)) return ShowError("Vui lòng nhập Email.");
            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return ShowError("Email không đúng định dạng.");

            if (string.IsNullOrEmpty(Gender)) return ShowError("Vui lòng chọn giới tính.");

            if (BirthDate == null) return ShowError("Vui lòng chọn ngày sinh.");
            if (HireDate == null) return ShowError("Vui lòng chọn ngày vào làm.");
            if (BirthDate.Value >= HireDate.Value) return ShowError("Ngày sinh phải nhỏ hơn ngày vào làm.");

            int age = HireDate.Value.Year - BirthDate.Value.Year;
            if (BirthDate.Value > HireDate.Value.AddYears(-age)) age--;
            if (age < 18) return ShowError($"Nhân viên chưa đủ 18 tuổi (Hiện tại: {age}).");

            return true;
        }

        private bool ShowError(string msg)
        {
            MessageBox.Show(msg, "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // --- Helpers ---
        private string GenerateEmployeeID()
        {
            var lastEmp = DataProvider.Ins.DB.Employees
                .Where(x => x.EmployeeID.StartsWith("NV"))
                .AsEnumerable()
                .OrderByDescending(x => x.EmployeeID)
                .FirstOrDefault();

            if (lastEmp == null) return "NV001";

            string lastId = lastEmp.EmployeeID;
            string numberPart = lastId.Substring(2);
            if (int.TryParse(numberPart, out int number))
            {
                return "NV" + (number + 1).ToString("D3");
            }
            return "NV" + Guid.NewGuid().ToString().Substring(0, 3).ToUpper();
        }

        private string GetFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Trim().Split(' ');
            return parts.Last();
        }

        private string ConvertToUnSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }

        private string CalculateSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}