using Martify.Helpers;
using Martify.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class AddSupplierVM : BaseVM, IDataErrorInfo
    {
        // Properties
        private string _supplierID;
        public string SupplierID
        {
            get => _supplierID;
            set { _supplierID = value; OnPropertyChanged(); }
        }

        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(); }
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        // Constructor
        public AddSupplierVM()
        {
            // Auto-generate supplier ID
            SupplierID = GenerateNewSupplierID();

            SaveCommand = new RelayCommand<Window>(
                (p) => CanSave(),
                (p) => SaveSupplier(p));

            CloseCommand = new RelayCommand<Window>(
                (p) => true,
                (p) => p?.Close());
        }

        // IDataErrorInfo Implementation
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(SupplierName):
                        error = SupplierValidator.ValidateSupplierName(SupplierName);
                        break;

                    case nameof(Phone):
                        error = SupplierValidator.ValidatePhone(Phone);
                        break;

                    case nameof(Email):
                        error = SupplierValidator.ValidateEmail(Email);
                        break;

                    case nameof(Address):
                        error = SupplierValidator.ValidateAddress(Address);
                        break;
                }
                return error;
            }
        }

        // Methods
        private bool CanSave()
        {
            // Check if all required fields are valid
            bool isNameValid = string.IsNullOrEmpty(this[nameof(SupplierName)]);
            bool isPhoneValid = string.IsNullOrEmpty(this[nameof(Phone)]);
            bool isEmailValid = string.IsNullOrEmpty(this[nameof(Email)]) || string.IsNullOrWhiteSpace(Email);
            bool isAddressValid = string.IsNullOrEmpty(this[nameof(Address)]) || string.IsNullOrWhiteSpace(Address);

            return isNameValid && isPhoneValid && isEmailValid && isAddressValid;
        }

        private void SaveSupplier(Window window)
        {
            try
            {
                // Double-check validation before saving
                if (!CanSave())
                {
                    MessageBox.Show("Vui lòng kiểm tra lại thông tin nhập vào!",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate supplier name
                var existingSupplier = DataProvider.Ins.DB.Suppliers
                    .FirstOrDefault(s => s.SupplierName.ToLower().Trim() == SupplierName.ToLower().Trim());

                if (existingSupplier != null)
                {
                    var result = MessageBox.Show(
                        $"Nhà cung cấp '{SupplierName}' đã tồn tại trong hệ thống.\n\nBạn có muốn tiếp tục thêm mới không?",
                        "Cảnh báo",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                // Check for duplicate phone number
                var existingPhone = DataProvider.Ins.DB.Suppliers
                    .FirstOrDefault(s => s.Phone == Phone.Trim());

                if (existingPhone != null)
                {
                    MessageBox.Show(
                        $"Số điện thoại '{Phone}' đã được sử dụng bởi nhà cung cấp '{existingPhone.SupplierName}'.",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Create new supplier
                var newSupplier = new Supplier
                {
                    SupplierID = this.SupplierID,
                    SupplierName = this.SupplierName.Trim(),
                    Phone = this.Phone.Trim(),
                    Address = string.IsNullOrWhiteSpace(this.Address) ? null : this.Address.Trim()
                };

                // Add to database
                DataProvider.Ins.DB.Suppliers.Add(newSupplier);
                DataProvider.Ins.DB.SaveChanges();

                // Show success message
                MessageBox.Show(
                    $"Đã thêm nhà cung cấp thành công!\n\n" +
                    $"Mã NCC: {newSupplier.SupplierID}\n" +
                    $"Tên: {newSupplier.SupplierName}\n" +
                    $"SĐT: {newSupplier.Phone}",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Close window with success result
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu nhà cung cấp:\n{ex.Message}\n\nChi tiết: {ex.InnerException?.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string GenerateNewSupplierID()
        {
            try
            {
                var supplierIds = DataProvider.Ins.DB.Suppliers
                    .Where(s => s.SupplierID.StartsWith("NCC"))
                    .Select(s => s.SupplierID)
                    .ToList();

                if (!supplierIds.Any())
                    return "NCC001";

                int maxId = 0;
                foreach (var id in supplierIds)
                {
                    if (id.Length > 3 && int.TryParse(id.Substring(3), out int num))
                    {
                        if (num > maxId)
                            maxId = num;
                    }
                }

                return "NCC" + (maxId + 1).ToString("D3");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating supplier ID: {ex.Message}");
                return "NCC001";
            }
        }
    }
}