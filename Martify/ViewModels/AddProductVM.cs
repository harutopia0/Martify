using Martify.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Martify.ViewModels
{
    // Kế thừa BaseVM (để dùng OnPropertyChanged) và IDataErrorInfo (để Validate lỗi hiển thị lên View)
    public class AddProductVM : BaseVM, IDataErrorInfo
    {
        // =================================================================================================
        // PHẦN 1: KHAI BÁO PROPERITES (BINDING VỚI VIEW)
        // =================================================================================================
                
        private string _productCode;
        public string ProductCode { get => _productCode; set { _productCode = value; OnPropertyChanged(); } }

        private string _productName;
        public string ProductName { get => _productName; set { _productName = value; OnPropertyChanged(); } }

        private string _unit;
        public string Unit { get => _unit; set { _unit = value; OnPropertyChanged(); } }

        private decimal? _price;
        public decimal? Price { get => _price; set { _price = value; OnPropertyChanged(); } }

        private int? _stockQuantity;
        public int? StockQuantity { get => _stockQuantity; set { _stockQuantity = value; OnPropertyChanged(); } }

        private string _categoryID;
        // internal backing for selected category
        public string CategoryID { get => _categoryID; set { _categoryID = value; OnPropertyChanged(); } }

        // Exposed to XAML: CategoryList and SelectedCategoryID
        private List<ProductCategory> _categoryList;
        public List<ProductCategory> CategoryList { get => _categoryList; set { _categoryList = value; OnPropertyChanged(); } }

        private string _selectedCategoryID;
        public string SelectedCategoryID
        {
            get => _selectedCategoryID;
            set
            {
                _selectedCategoryID = value;
                // keep internal CategoryID in sync
                CategoryID = value;
                OnPropertyChanged();
            }
        }

        // Suppliers for XAML
        private List<Supplier> _supplierList;
        public List<Supplier> SupplierList { get => _supplierList; set { _supplierList = value; OnPropertyChanged(); } }

        private string _selectedSupplierID;
        public string SelectedSupplierID { get => _selectedSupplierID; set { _selectedSupplierID = value; OnPropertyChanged(); } }

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
                case nameof(ProductName):
                    if (string.IsNullOrWhiteSpace(ProductName)) result = "Vui lòng nhập tên sản phẩm.";
                    else if (ProductName.Length > 100) result = "Tên sản phẩm không được quá 100 ký tự.";
                    // Thêm kiểm tra tên sản phẩm duy nhất (tùy theo nghiệp vụ)
                    else if (CheckProductNameExist(ProductName)) result = "Tên sản phẩm này đã tồn tại trong hệ thống.";
                    break;

                case nameof(Unit):
                    if (string.IsNullOrWhiteSpace(Unit)) result = "Vui lòng nhập đơn vị tính.";
                    else if (Unit.Length > 20) result = "Đơn vị tính không được quá 20 ký tự.";
                    break;

                case nameof(Price):
                    if (Price == null) result = "Vui lòng nhập giá bán.";
                    else if (Price < 0) result = "Giá bán không được âm.";
                    break;

                case nameof(StockQuantity):
                    if (StockQuantity == null) result = "Vui lòng nhập số lượng tồn kho.";
                    else if (StockQuantity < 0) result = "Số lượng tồn kho không được âm.";
                    break;

                case nameof(CategoryID):
                    if (string.IsNullOrWhiteSpace(CategoryID)) result = "Vui lòng chọn danh mục.";
                    break;
            }
            return result;
        }

        // =================================================================================================
        // PHẦN 3: COMMANDS
        // =================================================================================================
        public ICommand SaveCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand CategorySelectionChangedCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }
        public ICommand DragWindowCommand { get; set; }

        public AddProductVM()
        {
            // Ensure StockQuantity has a default so validation doesn't block when UI doesn't provide a field.
            _stockQuantity = 0;

            // Tải danh sách Category (Giả định DataProvider có thể lấy Categories)
            LoadCategories();
            LoadSuppliers();

            // Khởi tạo Commands
            CloseCommand = new RelayCommand<Window>((p) => { return true; }, (p) => p?.Close());

            SaveCommand = new RelayCommand<Window>((p) => { return true; }, (p) => SaveProduct(p));

            SelectImageCommand = new RelayCommand<object>((p) => true, (p) => SelectImage());

            CategorySelectionChangedCommand = new RelayCommand<ComboBox>((p) => p != null, (p) =>
            {
                if (p.SelectedItem is ProductCategory selectedCategory)
                    SelectedCategoryID = selectedCategory.CategoryID;
            });

            DragWindowCommand = new RelayCommand<Window>((p) => p != null, (p) => { try { p.DragMove(); } catch { } });
        }

        void LoadCategories()
        {
            // Thay thế bằng logic truy vấn DB thực tế
            CategoryList = DataProvider.Ins.DB.ProductCategories.ToList();
            // Keep internal CategoryID in sync if a selected value exists
            if (CategoryList.Any() && string.IsNullOrEmpty(SelectedCategoryID))
            {
                // do not auto-select; leave selection to user — or uncomment to auto-select:
                // SelectedCategoryID = CategoryList.First().CategoryID;
            }
        }

        void LoadSuppliers()
        {
            SupplierList = DataProvider.Ins.DB.Suppliers.ToList();
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
        // PHẦN 4: HÀM LƯU SẢN PHẨM
        // =================================================================================================
        void SaveProduct(Window p)
        {
            // 1. Bật cờ đã bấm nút Lưu -> Refresh UI để hiện lỗi đỏ
            _isSaveClicked = true;
            OnPropertyChanged(null);

            // 2. Kiểm tra xem còn lỗi nào không
            if (!IsValid()) return;

            // 3. Sinh mã và xử lý lưu
            string newProductID = GenerateProductID();
            string dbPath = null;

            if (!string.IsNullOrEmpty(_sourceImageFile))
            {
                dbPath = HandleImageSave(newProductID, _sourceImageFile);
                if (dbPath == "ERROR") return;
            }

            var newProduct = new Models.Product()
            {
                ProductID = newProductID,
                ProductName = ProductName,
                Unit = Unit,
                Price = Price.Value,
                StockQuantity = StockQuantity.Value,
                ImagePath = dbPath,
                CategoryID = CategoryID // Đã chọn ở ComboBox
            };

            try
            {
                DataProvider.Ins.DB.Products.Add(newProduct);
                DataProvider.Ins.DB.SaveChanges();

                MessageBox.Show($"Thêm sản phẩm thành công!\n\n- Mã SP: {newProductID}\n- Tên SP: {ProductName}",
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
            string[] properties = { nameof(ProductName), nameof(Unit), nameof(Price), nameof(StockQuantity), nameof(CategoryID) };
            foreach (var prop in properties)
            {
                if (!string.IsNullOrEmpty(GetValidationError(prop))) return false;
            }
            return true;
        }

        // Helper: Kiểm tra Tên sản phẩm đã tồn tại chưa (Giả định không trùng tên)
        private bool CheckProductNameExist(string productName)
        {
            return DataProvider.Ins.DB.Products.Any(x => x.ProductName == productName);
        }

        private string HandleImageSave(string productId, string sourceFile)
        {
            try
            {
                string fileExt = Path.GetExtension(sourceFile);
                string fileName = $"{productId}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";

                string binFolder = AppDomain.CurrentDomain.BaseDirectory;

                // A. Copy vào BIN (Đảm bảo đường dẫn)
                string binAssetsPath = Path.Combine(binFolder, "Assets", "Product");
                if (!Directory.Exists(binAssetsPath)) Directory.CreateDirectory(binAssetsPath);

                string destBinFile = Path.Combine(binAssetsPath, fileName);
                File.Copy(sourceFile, destBinFile, true);

                // B. Copy vào SOURCE CODE (Tùy chọn, giống AddEmployeeVM)
                try
                {
                    string projectFolder = Path.GetFullPath(Path.Combine(binFolder, @"..\..\..\"));
                    string sourcePath = Path.Combine(projectFolder, "Assets", "Product");

                    if (Directory.Exists(Path.Combine(projectFolder, "Assets")))
                    {
                        if (!Directory.Exists(sourcePath)) Directory.CreateDirectory(sourcePath);
                        File.Copy(sourceFile, Path.Combine(sourcePath, fileName), true);
                    }
                }
                catch { /* Bỏ qua lỗi khi không copy được vào thư mục Source Code */ }

                return Path.Combine("Assets", "Product", fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu ảnh: " + ex.Message);
                return "ERROR";
            }
        }

        private string GenerateProductID()
        {
            // Logic sinh ID sản phẩm (Ví dụ: SP001)
            var productIds = DataProvider.Ins.DB.Products
                .Where(x => x.ProductID.StartsWith("SP"))
                .Select(x => x.ProductID).ToList();

            if (productIds.Count == 0) return "SP001";

            int maxId = 0;
            foreach (var id in productIds)
            {
                if (id.Length > 2 && int.TryParse(id.Substring(2), out int num))
                    if (num > maxId) maxId = num;
            }
            return "SP" + (maxId + 1).ToString("D3");
        }
    }
}