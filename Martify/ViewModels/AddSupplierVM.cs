using Martify.Models;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class AddSupplierVM : BaseVM
    {
        // Các thuộc tính cho nhà cung cấp mới
        private string _supplierID;
        public string SupplierID { get => _supplierID; set { _supplierID = value; OnPropertyChanged(); } }

        private string _supplierName;
        public string SupplierName { get => _supplierName; set { _supplierName = value; OnPropertyChanged(); } }

        private string _phone;
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(); } }

        private string _address;
        public string Address { get => _address; set { _address = value; OnPropertyChanged(); } }
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        public AddSupplierVM()
        {
            // Tự động sinh mã nhà cung cấp khi mở cửa sổ
            SupplierID = GenerateNewSupplierID();

            SaveCommand = new RelayCommand<Window>((p) => CanSave(), (p) => SaveSupplier(p));
            CloseCommand = new RelayCommand<Window>((p) => true, (p) => p.Close());
        }

        private bool CanSave()
        {
            // Kiểm tra không để trống tên và số điện thoại
            return !string.IsNullOrWhiteSpace(SupplierName) && !string.IsNullOrWhiteSpace(Phone);
        }

        private void SaveSupplier(Window window)
        {
            try
            {
                var newSupplier = new Supplier
                {
                    SupplierID = this.SupplierID,
                    SupplierName = this.SupplierName,
                    Phone = this.Phone,
                    Address = this.Address,
                };

                DataProvider.Ins.DB.Suppliers.Add(newSupplier);
                DataProvider.Ins.DB.SaveChanges();

                MessageBox.Show("Thêm nhà cung cấp thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                if (window != null)
                {
                    window.DialogResult = true; // Để báo cho ImportProductsVM biết là đã lưu thành công
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateNewSupplierID()
        {
            try
            {
                var ids = DataProvider.Ins.DB.Suppliers
                    .Where(s => s.SupplierID.StartsWith("NCC"))
                    .Select(s => s.SupplierID)
                    .ToList();

                if (!ids.Any()) return "NCC001";

                int maxNumber = ids.Select(id => int.Parse(id.Substring(3))).Max();
                return "NCC" + (maxNumber + 1).ToString("D3");
            }
            catch { return "NCC" + Guid.NewGuid().ToString().Substring(0, 5); }
        }
    }
}