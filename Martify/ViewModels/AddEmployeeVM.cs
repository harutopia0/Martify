using Martify.Models;
using Martify.Views;
using System;
using System.Collections.Generic;
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

        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand GenderSelectionChangedCommand { get; set; }

        public AddEmployeeVM()
        {

            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) =>
            {
                p?.Close();
            });

            SaveCommand = new RelayCommand<Window>((p) => { return true; }, (p) =>
            {
                if (!ValidateInput()) return;

                string newEmpId = GenerateEmployeeID();

                var newEmployee = new Models.Employee()
                {
                    EmployeeID = newEmpId,
                    FullName = FullName,
                    Address = Address,
                    Phone = Phone,
                    Email = Email,
                    Gender = Gender,
                    BirthDate = BirthDate.Value,
                    HireDate = DateTime.Now,
                };

                string firstName = GetFirstName(FullName);
                string username = (ConvertToUnSign(firstName) + newEmpId).ToLower();

                string rawPassword = BirthDate.Value.ToString("ddMMyyyy");
                string hashPassword = CalculateSHA256(rawPassword);

                var newAccount = new Account()
                {
                    Username = username,
                    HashPassword = hashPassword,
                    Role = 0,
                    EmployeeID = newEmpId
                };

                newEmployee.Accounts = new List<Account> { newAccount };

                try
                {
                    DataProvider.Ins.DB.Employees.Add(newEmployee);
                    DataProvider.Ins.DB.SaveChanges();

                    MessageBox.Show($"Thêm thành công!\n- Mã NV: {newEmpId}\n- Tài khoản: {username}\n- Mật khẩu: {rawPassword}",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    p?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            GenderSelectionChangedCommand = new RelayCommand<ListBox>((p) => { return p != null; }, (p) =>
            {
                if (p.SelectedItem == null) return;
                var selectedItem = p.SelectedItem as ListBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    Gender = selectedItem.Tag.ToString();
                }
            });
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                MessageBox.Show("Vui lòng nhập họ và tên.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(Address))
            {
                MessageBox.Show("Vui lòng nhập nơi cư trú.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrEmpty(Phone) || !Regex.IsMatch(Phone, @"^[0-9]+$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ (chỉ được nhập số).", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                MessageBox.Show("Vui lòng nhập Email.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!Regex.IsMatch(Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
            {
                MessageBox.Show("Email không đúng định dạng.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrEmpty(Gender))
            {
                MessageBox.Show("Vui lòng chọn giới tính.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (BirthDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày sinh.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (DataProvider.Ins.DB.Employees.Any(x => x.Phone == Phone))
            {
                MessageBox.Show("Số điện thoại này đã được sử dụng.", "Trùng dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private string GenerateEmployeeID()
        {
            var lastNvEmp = DataProvider.Ins.DB.Employees
                .Where(x => x.EmployeeID.StartsWith("NV"))
                .OrderByDescending(x => x.EmployeeID)
                .FirstOrDefault();

            if (lastNvEmp == null) return "NV001";

            string lastId = lastNvEmp.EmployeeID;
            string numberPart = lastId.Substring(2);
            if (int.TryParse(numberPart, out int number))
            {
                return "NV" + (number + 1).ToString("D3");
            }
            return "NV" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
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