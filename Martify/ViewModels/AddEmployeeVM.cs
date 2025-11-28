using Martify.Models;
using Martify.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    // Kế thừa BaseVM (để dùng OnPropertyChanged) và IDataErrorInfo (để Validate lỗi hiển thị lên View)
    public class AddEmployeeVM : BaseVM, IDataErrorInfo
    {
        // =================================================================================================
        // PHẦN 1: KHAI BÁO PROPERITES (BINDING VỚI VIEW)
        // =================================================================================================

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

        // --- Xử lý Logic chéo giữa Ngày sinh và Ngày vào làm ---
        private DateTime? _birthDate;
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                _birthDate = value;
                OnPropertyChanged();
                // Logic: Khi người dùng đổi Ngày Sinh -> Cần kích hoạt kiểm tra lại Ngày Vào Làm.
                OnPropertyChanged(nameof(HireDate));
            }
        }

        private DateTime? _hireDate;
        public DateTime? HireDate
        {
            get => _hireDate;
            set
            {
                _hireDate = value;
                OnPropertyChanged();
                // Logic: Khi người dùng đổi Ngày Vào Làm -> Cần kích hoạt kiểm tra lại Ngày Sinh.
                OnPropertyChanged(nameof(BirthDate));
            }
        }

        // --- Xử lý hiển thị ảnh ---
        private string _selectedImagePath;
        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set { _selectedImagePath = value; OnPropertyChanged(); }
        }

        private string _sourceImageFile;

        // =================================================================================================
        // PHẦN 2: CƠ CHẾ LAZY VALIDATION (VALIDATE TRỄ)
        // =================================================================================================

        // Biến cờ (Flag) xác định đã bấm nút Lưu chưa
        private bool _isSaveClicked = false;

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                // Bước 1: Lấy lỗi thực tế
                string error = GetValidationError(columnName);

                // Bước 2: Nếu không có lỗi -> Trả về null (Xanh)
                if (string.IsNullOrEmpty(error)) return null;

                // Bước 3: Nếu có lỗi nhưng chưa bấm Lưu -> Trả về null (Ẩn lỗi)
                if (!_isSaveClicked) return null;

                // Bước 4: Nếu có lỗi và đã bấm Lưu -> Hiện đỏ
                return error;
            }
        }

        // Hàm chứa toàn bộ quy tắc (Rules) kiểm tra dữ liệu
        private string GetValidationError(string columnName)
        {
            string result = null;
            switch (columnName)
            {
                case nameof(FullName):
                    if (string.IsNullOrWhiteSpace(FullName)) result = "Vui lòng nhập họ và tên.";
                    else if (!Regex.IsMatch(FullName, @"^[\p{L}\s]+$")) result = "Họ tên không được chứa số hoặc ký tự đặc biệt.";
                    break;

                case nameof(Address):
                    if (string.IsNullOrWhiteSpace(Address)) result = "Vui lòng nhập nơi cư trú.";
                    break;

                case nameof(Phone):
                    if (string.IsNullOrEmpty(Phone)) result = "Vui lòng nhập SĐT.";
                    else if (!Regex.IsMatch(Phone, @"^[0-9]+$")) result = "SĐT chỉ được chứa số.";
                    else if (Phone.Length < 9 || Phone.Length > 12) result = "SĐT phải từ 9-12 số.";
                    // Kiểm tra SĐT duy nhất
                    else if (CheckPhoneExist(Phone)) result = "SĐT này đã tồn tại trong hệ thống.";
                    break;

                case nameof(Email):
                    if (string.IsNullOrWhiteSpace(Email)) result = "Vui lòng nhập Email.";
                    else if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) result = "Email không đúng định dạng.";
                    // Kiểm tra Email duy nhất (MỚI THÊM)
                    else if (CheckEmailExist(Email)) result = "Email này đã tồn tại trong hệ thống.";
                    break;

                case nameof(Gender):
                    if (string.IsNullOrEmpty(Gender)) result = "Vui lòng chọn giới tính.";
                    break;

                case nameof(BirthDate):
                    if (BirthDate == null) result = "Vui lòng chọn ngày sinh.";
                    else if (BirthDate.Value.Date > DateTime.Now.Date) result = "Ngày sinh không được lớn hơn hiện tại.";
                    else if (HireDate != null && BirthDate.Value >= HireDate.Value) result = "Ngày sinh phải nhỏ hơn ngày vào làm.";
                    break;

                case nameof(HireDate):
                    if (HireDate == null) result = "Vui lòng chọn ngày vào làm.";
                    else if (HireDate.Value.Date > DateTime.Now.Date) result = "Ngày vào làm không được lớn hơn hiện tại.";
                    else if (BirthDate != null)
                    {
                        if (HireDate.Value <= BirthDate.Value)
                            result = "Ngày vào làm phải lớn hơn ngày sinh.";
                        else
                        {
                            int age = HireDate.Value.Year - BirthDate.Value.Year;
                            if (BirthDate.Value > HireDate.Value.AddYears(-age)) age--;

                            if (age < 18)
                                result = $"Chưa đủ 18 tuổi (Tính đến ngày vào làm là {age} tuổi).";
                        }
                    }
                    break;
            }
            return result;
        }

        // =================================================================================================
        // PHẦN 3: COMMANDS
        // =================================================================================================
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand GenderSelectionChangedCommand { get; set; }
        public ICommand DragWindowCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }

        public AddEmployeeVM()
        {
            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) => p?.Close());

            SaveCommand = new RelayCommand<Window>((p) => { return true; }, (p) => SaveEmployee(p));

            GenderSelectionChangedCommand = new RelayCommand<ListBox>((p) => { return p != null; }, (p) =>
            {
                if (p.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag != null)
                    Gender = selectedItem.Tag.ToString();
            });

            DragWindowCommand = new RelayCommand<Window>((p) => p != null, (p) => { try { p.DragMove(); } catch { } });

            SelectImageCommand = new RelayCommand<object>((p) => true, (p) => SelectImage());
        }

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

        // =================================================================================================
        // PHẦN 4: HÀM LƯU NHÂN VIÊN
        // =================================================================================================
        void SaveEmployee(Window p)
        {
            // 1. Bật cờ đã bấm nút Lưu -> Refresh UI để hiện lỗi đỏ
            _isSaveClicked = true;
            OnPropertyChanged(null);

            // 2. Kiểm tra xem còn lỗi nào không
            if (!IsValid()) return;

            // 3. Sinh mã và xử lý lưu
            string newEmpId = GenerateEmployeeID();
            string dbPath = null;

            if (!string.IsNullOrEmpty(_sourceImageFile))
            {
                dbPath = HandleImageSave(newEmpId, _sourceImageFile);
                if (dbPath == "ERROR") return;
            }

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
                ImagePath = dbPath
            };

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
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =================================================================================================
        // PHẦN 5: CÁC HÀM HỖ TRỢ (HELPERS)
        // =================================================================================================

        private bool IsValid()
        {
            string[] properties = { nameof(FullName), nameof(Address), nameof(Phone), nameof(Email), nameof(Gender), nameof(BirthDate), nameof(HireDate) };
            foreach (var prop in properties)
            {
                if (!string.IsNullOrEmpty(GetValidationError(prop))) return false;
            }
            return true;
        }

        // Helper: Kiểm tra SĐT đã tồn tại chưa
        private bool CheckPhoneExist(string phone)
        {
            return DataProvider.Ins.DB.Employees.Any(x => x.Phone == phone);
        }

        // Helper: Kiểm tra Email đã tồn tại chưa
        private bool CheckEmailExist(string email)
        {
            // Kiểm tra trong DB xem có ai có Email trùng với email đang nhập không
            return DataProvider.Ins.DB.Employees.Any(x => x.Email == email);
        }

        private string HandleImageSave(string empId, string sourceFile)
        {
            try
            {
                string fileExt = Path.GetExtension(sourceFile);
                string fileName = $"{empId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";

                string binFolder = AppDomain.CurrentDomain.BaseDirectory;

                // A. Copy vào BIN
                string binAssetsPath = Path.Combine(binFolder, "Assets", "Employee");
                if (!Directory.Exists(binAssetsPath)) Directory.CreateDirectory(binAssetsPath);

                string destBinFile = Path.Combine(binAssetsPath, fileName);
                File.Copy(sourceFile, destBinFile, true);

                // B. Copy vào SOURCE CODE
                try
                {
                    string projectFolder = Path.GetFullPath(Path.Combine(binFolder, @"..\..\..\"));
                    string sourcePath = Path.Combine(projectFolder, "Assets", "Employee");

                    if (Directory.Exists(Path.Combine(projectFolder, "Assets")))
                    {
                        if (!Directory.Exists(sourcePath)) Directory.CreateDirectory(sourcePath);
                        File.Copy(sourceFile, Path.Combine(sourcePath, fileName), true);
                    }
                }
                catch { }

                return Path.Combine("Assets", "Employee", fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu ảnh: " + ex.Message);
                return "ERROR";
            }
        }

        private string GenerateEmployeeID()
        {
            var empIds = DataProvider.Ins.DB.Employees
                .Where(x => x.EmployeeID.StartsWith("NV"))
                .Select(x => x.EmployeeID).ToList();

            if (empIds.Count == 0) return "NV001";

            int maxId = 0;
            foreach (var id in empIds)
            {
                if (id.Length > 2 && int.TryParse(id.Substring(2), out int num))
                    if (num > maxId) maxId = num;
            }
            return "NV" + (maxId + 1).ToString("D3");
        }

        private string GetFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            return fullName.Trim().Split(' ').Last();
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
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}