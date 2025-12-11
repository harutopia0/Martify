using Martify.Helpers;
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
    public class AddEmployeeVM : BaseVM, IDataErrorInfo
    {
        // ... (Giữ nguyên phần khai báo Properties: FullName, Address, v.v...)
        private string _fullName; public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
        private string _address; public string Address { get => _address; set { _address = value; OnPropertyChanged(); } }
        private string _phone; public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(); } }
        private string _email; public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        private string _gender; public string Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }

        private DateTime? _birthDate;
        public DateTime? BirthDate { get => _birthDate; set { _birthDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(HireDate)); } }

        private DateTime? _hireDate;
        public DateTime? HireDate { get => _hireDate; set { _hireDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(BirthDate)); } }

        private string _selectedImagePath; public string SelectedImagePath { get => _selectedImagePath; set { _selectedImagePath = value; OnPropertyChanged(); } }
        private string _sourceImageFile;

        // --- VALIDATION SỬ DỤNG HELPER ---
        private bool _isSaveClicked = false;
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;
                // Gọi sang file Helper để check lỗi
                switch (columnName)
                {
                    case nameof(FullName): error = EmployeeValidator.CheckFullName(FullName); break;
                    case nameof(Address): error = EmployeeValidator.CheckAddress(Address); break;
                    // Truyền null cho excludeId vì đây là Thêm Mới
                    case nameof(Phone): error = EmployeeValidator.CheckPhone(Phone, null); break;
                    case nameof(Email): error = EmployeeValidator.CheckEmail(Email, null); break;
                    case nameof(Gender): error = EmployeeValidator.CheckGender(Gender); break;
                    case nameof(BirthDate): error = EmployeeValidator.CheckBirthDate(BirthDate, HireDate); break;
                    case nameof(HireDate): error = EmployeeValidator.CheckHireDate(HireDate, BirthDate); break;
                }

                if (string.IsNullOrEmpty(error)) return null;
                if (!_isSaveClicked) return null;
                return error;
            }
        }

        // Hàm check tổng (Cũng gọi Helper)
        private bool IsValid()
        {
            // Kiểm tra từng trường, nếu có bất kỳ lỗi nào trả về khác null -> False
            if (EmployeeValidator.CheckFullName(FullName) != null) return false;
            if (EmployeeValidator.CheckAddress(Address) != null) return false;
            if (EmployeeValidator.CheckPhone(Phone, null) != null) return false;
            if (EmployeeValidator.CheckEmail(Email, null) != null) return false;
            if (EmployeeValidator.CheckGender(Gender) != null) return false;
            if (EmployeeValidator.CheckBirthDate(BirthDate, HireDate) != null) return false;
            if (EmployeeValidator.CheckHireDate(HireDate, BirthDate) != null) return false;
            return true;
        }

        // ... (Giữ nguyên phần Commands, Constructor, SaveEmployee, HandleImageSave, v.v...)

        // Command và Constructor
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

        void SaveEmployee(Window p)
        {
            _isSaveClicked = true;
            OnPropertyChanged(null); // Refresh UI để hiện lỗi

            if (!IsValid()) return; // Dừng nếu có lỗi

            // ... (Logic lưu DB giữ nguyên như cũ) ...
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

        // Các hàm Helper private giữ nguyên
        private string HandleImageSave(string empId, string sourceFile)
        {
            try
            {
                string fileExt = Path.GetExtension(sourceFile);
                string fileName = $"{empId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
                string binFolder = AppDomain.CurrentDomain.BaseDirectory;
                string binAssetsPath = Path.Combine(binFolder, "Assets", "Employee");
                if (!Directory.Exists(binAssetsPath)) Directory.CreateDirectory(binAssetsPath);
                string destBinFile = Path.Combine(binAssetsPath, fileName);
                File.Copy(sourceFile, destBinFile, true);
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
            var empIds = DataProvider.Ins.DB.Employees.Where(x => x.EmployeeID.StartsWith("NV")).Select(x => x.EmployeeID).ToList();
            if (empIds.Count == 0) return "NV001";
            int maxId = 0;
            foreach (var id in empIds) { if (id.Length > 2 && int.TryParse(id.Substring(2), out int num)) if (num > maxId) maxId = num; }
            return "NV" + (maxId + 1).ToString("D3");
        }
        private string GetFirstName(string fullName) { if (string.IsNullOrWhiteSpace(fullName)) return ""; return fullName.Trim().Split(' ').Last(); }
        private string ConvertToUnSign(string text) { if (string.IsNullOrEmpty(text)) return ""; Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+"); string temp = text.Normalize(NormalizationForm.FormD); return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D'); }
        private string CalculateSHA256(string rawData) { using (SHA256 sha256Hash = SHA256.Create()) { byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData)); StringBuilder builder = new StringBuilder(); for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2")); return builder.ToString(); } }
    }
}