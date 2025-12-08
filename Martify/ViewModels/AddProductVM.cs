using Martify.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class AddProductVM : BaseVM, IDataErrorInfo
    {
        // =================================================================================================
        // THUỘC TÍNH
        // =================================================================================================

        private string _productName;
        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        private string _unit;
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        private decimal? _price;
        public decimal? Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private int? _stockQuantity;
        public int? StockQuantity
        {
            get => _stockQuantity;
            set { _stockQuantity = value; OnPropertyChanged(); }
        }

        private string _categoryID;
        public string CategoryID
        {
            get => _categoryID;
            set { _categoryID = value; OnPropertyChanged(); }
        }

        private List<ProductCategory> _categoryList;
        public List<ProductCategory> CategoryList
        {
            get => _categoryList;
            set { _categoryList = value; OnPropertyChanged(); }
        }

        private string _selectedCategoryID;
        public string SelectedCategoryID
        {
            get => _selectedCategoryID;
            set
            {
                _selectedCategoryID = value;
                CategoryID = value; // Giữ đồng bộ
                OnPropertyChanged();
            }
        }

        private List<Supplier> _supplierList;
        public List<Supplier> SupplierList
        {
            get => _supplierList;
            set { _supplierList = value; OnPropertyChanged(); }
        }

        private string _selectedSupplierID;
        public string SelectedSupplierID
        {
            get => _selectedSupplierID;
            set { _selectedSupplierID = value; OnPropertyChanged(); }
        }

        private string _selectedImagePath;
        public string SelectedImagePath
        {
            get => _selectedImagePath;
            set { _selectedImagePath = value; OnPropertyChanged(); }
        }

        private string _sourceImageFile;

        // =================================================================================================
        // XÁC THỰC
        // =================================================================================================

        private bool _isSaveClicked = false;

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = GetValidationError(columnName);
                if (string.IsNullOrEmpty(error)) return null;
                if (!_isSaveClicked) return null;
                return error;
            }
        }

        private string GetValidationError(string columnName)
        {
            string result = null;
            switch (columnName)
            {
                case nameof(ProductName):
                    if (string.IsNullOrWhiteSpace(ProductName))
                        result = "Vui lòng nhập tên sản phẩm.";
                    else if (ProductName.Length > 100)
                        result = "Tên sản phẩm không được quá 100 ký tự.";
                    else if (CheckProductNameExist(ProductName))
                        result = "Tên sản phẩm này đã tồn tại.";
                    break;

                case nameof(Unit):
                    if (string.IsNullOrWhiteSpace(Unit))
                        result = "Vui lòng nhập đơn vị tính.";
                    else if (Unit.Length > 20)
                        result = "Đơn vị tính không được quá 20 ký tự.";
                    break;

                case nameof(Price):
                    if (Price == null)
                        result = "Vui lòng nhập giá bán.";
                    else if (Price <= 0)
                        result = "Giá bán phải lớn hơn 0.";
                    break;

                case nameof(StockQuantity):
                    if (StockQuantity == null)
                        result = "Vui lòng nhập số lượng.";
                    else if (StockQuantity < 0)
                        result = "Số lượng không được âm.";
                    break;

                case nameof(CategoryID):
                case nameof(SelectedCategoryID):
                    if (string.IsNullOrWhiteSpace(SelectedCategoryID))
                        result = "Vui lòng chọn danh mục.";
                    break;
            }
            return result;
        }

        // =================================================================================================
        // LỆNH
        // =================================================================================================

        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }

        // =================================================================================================
        // HÀM KHỞI TẠO
        // =================================================================================================

        public AddProductVM()
        {
            // Khởi tạo giá trị mặc định
            StockQuantity = 0;

            // Tải dữ liệu
            LoadCategories();
            LoadSuppliers();

            // Khởi tạo lệnh
            SaveCommand = new RelayCommand<Window>(
                (p) => true,
                (p) => SaveProduct(p));

            CloseCommand = new RelayCommand<Window>(
                (p) => true,
                (p) => p?.Close());

            SelectImageCommand = new RelayCommand<object>(
                (p) => true,
                (p) => SelectImage());
        }

        // =================================================================================================
        // PHƯƠNG THỨC
        // =================================================================================================

        private void LoadCategories()
        {
            try
            {
                CategoryList = DataProvider.Ins.DB.ProductCategories.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách danh mục: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                SupplierList = DataProvider.Ins.DB.Suppliers.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Chọn ảnh sản phẩm"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _sourceImageFile = openFileDialog.FileName;
                SelectedImagePath = _sourceImageFile;
            }
        }

        private void SaveProduct(Window window)
        {
            // Hiển thị xác thực
            _isSaveClicked = true;
            OnPropertyChanged(null); // Cập nhật lại tất cả thuộc tính

            // Xác thực tất cả trường
            if (!IsValid())
            {
                MessageBox.Show("Vui lòng kiểm tra lại thông tin nhập vào.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Tạo mã sản phẩm mới
                string newProductID = GenerateProductID();

                // Xử lý lưu ảnh
                string dbImagePath = null;
                if (!string.IsNullOrEmpty(_sourceImageFile))
                {
                    dbImagePath = HandleImageSave(newProductID, _sourceImageFile);
                    if (dbImagePath == "ERROR")
                    {
                        MessageBox.Show("Không thể lưu ảnh sản phẩm.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Tạo sản phẩm mới
                var newProduct = new Product
                {
                    ProductID = newProductID,
                    ProductName = ProductName.Trim(),
                    Unit = Unit.Trim(),
                    Price = Price.Value,
                    StockQuantity = StockQuantity.Value,
                    ImagePath = dbImagePath,
                    CategoryID = SelectedCategoryID,
                };

                // Lưu vào cơ sở dữ liệu
                DataProvider.Ins.DB.Products.Add(newProduct);
                DataProvider.Ins.DB.SaveChanges();

                MessageBox.Show(
                    $"Thêm sản phẩm thành công!\n\n" +
                    $"Mã SP: {newProductID}\n" +
                    $"Tên: {ProductName}",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                window?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu sản phẩm: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool IsValid()
        {
            string[] properties =
            {
                nameof(ProductName),
                nameof(Unit),
                nameof(Price),
                nameof(StockQuantity),
                nameof(SelectedCategoryID)
            };

            foreach (var prop in properties)
            {
                if (!string.IsNullOrEmpty(GetValidationError(prop)))
                    return false;
            }

            return true;
        }

        private bool CheckProductNameExist(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return false;

            try
            {
                return DataProvider.Ins.DB.Products
                    .Any(p => p.ProductName.Trim().ToLower() == productName.Trim().ToLower());
            }
            catch
            {
                return false;
            }
        }

        private string HandleImageSave(string productId, string sourceFile)
        {
            try
            {
                string fileExt = Path.GetExtension(sourceFile);
                string fileName = $"{productId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";

                string binFolder = AppDomain.CurrentDomain.BaseDirectory;

                // Sao chép vào thư mục BIN
                string binAssetsPath = Path.Combine(binFolder, "Assets", "Product");
                if (!Directory.Exists(binAssetsPath))
                    Directory.CreateDirectory(binAssetsPath);

                string destBinFile = Path.Combine(binAssetsPath, fileName);
                File.Copy(sourceFile, destBinFile, true);

                // Thử sao chép vào thư mục nguồn (tùy chọn)
                try
                {
                    string projectFolder = Path.GetFullPath(Path.Combine(binFolder, @"..\..\..\"));
                    string sourcePath = Path.Combine(projectFolder, "Assets", "Product");

                    if (Directory.Exists(Path.Combine(projectFolder, "Assets")))
                    {
                        if (!Directory.Exists(sourcePath))
                            Directory.CreateDirectory(sourcePath);

                        string destSourceFile = Path.Combine(sourcePath, fileName);
                        File.Copy(sourceFile, destSourceFile, true);
                    }
                }
                catch
                {
                    // Bỏ qua lỗi khi sao chép vào thư mục nguồn
                }

                // Trả về đường dẫn tương đối để lưu vào cơ sở dữ liệu
                return Path.Combine("Assets", "Product", fileName).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu ảnh: {ex.Message}");
                return "ERROR";
            }
        }

        private string GenerateProductID()
        {
            try
            {
                var productIds = DataProvider.Ins.DB.Products
                    .Where(x => x.ProductID.StartsWith("SP"))
                    .Select(x => x.ProductID)
                    .ToList();

                if (productIds.Count == 0)
                    return "SP001";

                int maxId = 0;
                foreach (var id in productIds)
                {
                    if (id.Length > 2 && int.TryParse(id.Substring(2), out int num))
                    {
                        if (num > maxId)
                            maxId = num;
                    }
                }

                return "SP" + (maxId + 1).ToString("D3");
            }
            catch
            {
                return "SP001";
            }
        }
    }
}