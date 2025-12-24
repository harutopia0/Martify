using Martify.Models;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
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
        private List<string> _unitList;
        public List<string> UnitList
        {
            get => _unitList;
            set { _unitList = value; OnPropertyChanged(); }
        }

        private string _selectedCategoryID;
        public string SelectedCategoryID
        {
            get => _selectedCategoryID;
            set
            {
                if (_selectedCategoryID == value) return;
                _selectedCategoryID = value;
                CategoryID = value; // Giữ đồng bộ

                // When an existing category is selected, update the displayed text to its name (no recursion)
                if (!string.IsNullOrWhiteSpace(_selectedCategoryID) && CategoryList != null)
                {
                    var cat = CategoryList.FirstOrDefault(c => c.CategoryID == _selectedCategoryID);
                    var catName = cat?.CategoryName;
                    if (!string.Equals(SelectedCategoryText, catName, StringComparison.Ordinal))
                    {
                        _selectedCategoryText = catName;
                        OnPropertyChanged(nameof(SelectedCategoryText));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(SelectedCategoryText))
                    {
                        _selectedCategoryText = null;
                        OnPropertyChanged(nameof(SelectedCategoryText));
                    }
                }

                OnPropertyChanged();
            }
        }

        private string _selectedUnit;

        public string SelectedUnit
        {
            get => _selectedUnit;
            set { _selectedUnit = value; OnPropertyChanged(); }
        }

        public string SelectedUnitText
        {   get => SelectedUnit;
            set
            {
                SelectedUnit = value;
                Unit = value; // Giữ đồng bộ
                OnPropertyChanged();
            }
        }

        // backing field for editable category text
        private string _selectedCategoryText;
        public string SelectedCategoryText
        {
            get => _selectedCategoryText;
            set
            {
                if (_selectedCategoryText == value) return;
                _selectedCategoryText = value;

                // if typed text matches an existing CategoryName, sync SelectedCategoryID
                if (!string.IsNullOrWhiteSpace(_selectedCategoryText) && CategoryList != null)
                {
                    var match = CategoryList
                        .FirstOrDefault(p => p.CategoryName.Trim().ToLower() == SelectedCategoryText.Trim().ToLower());

                    if (match != null)
                    {
                        if (!string.Equals(SelectedCategoryID, match.CategoryID, StringComparison.Ordinal))
                            SelectedCategoryID = match.CategoryID;
                    }
                    else
                    {
                        // user typed a new category name -> clear SelectedCategoryID so SaveProduct will create it
                        if (!string.IsNullOrEmpty(SelectedCategoryID))
                            SelectedCategoryID = null;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(SelectedCategoryID))
                        SelectedCategoryID = null;
                }

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
                case nameof(SelectedCategoryText):
                    // require either an existing selected category id OR a typed category name
                    if (string.IsNullOrWhiteSpace(SelectedCategoryID) && string.IsNullOrWhiteSpace(SelectedCategoryText))
                        result = "Vui lòng chọn hoặc nhập danh mục.";
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
            LoadUnit();

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
                CategoryList = DataProvider.Ins.DB.ProductCategories
                    .OrderBy(c => c.CategoryName)
                    .ToList();
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
        private void LoadUnit()
        {
            try
            {
                // Get distinct units from products and assign to UnitList
                UnitList = DataProvider.Ins.DB.Products
                    .Select(p => p.Unit)
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn vị: {ex.Message}",
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
                // Tạo đơn vị tính mới
                string newUnit = SelectedUnitText?.Trim() ?? string.Empty;

                // Category logic: user can type new category name or select existing category
                string typedCategory = SelectedCategoryText?.Trim();
                string chosenCategoryID = SelectedCategoryID; // may be null if user typed new category

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

                // Unit handling (add new if typed)
                if (!string.IsNullOrEmpty(newUnit))
                {
                    bool unitExists = UnitList.Any(u => string.Equals(u?.Trim(), newUnit, StringComparison.OrdinalIgnoreCase));
                    if (!unitExists)
                    {
                        UnitList.Add(newUnit);
                    }
                    else
                    {
                        newUnit = Unit?.Trim() ?? newUnit;
                    }
                }

                // Category handling: if user typed a category name that doesn't exist, create it
                if (!string.IsNullOrWhiteSpace(typedCategory) && string.IsNullOrWhiteSpace(chosenCategoryID))
                {
                    // check by name
                    var existing = DataProvider.Ins.DB.ProductCategories
                        .FirstOrDefault(c => c.CategoryName.Trim().Equals(typedCategory, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        chosenCategoryID = existing.CategoryID;
                    }
                    else
                    {
                        // create new category
                        string newCatId = GenerateCategoryID();
                        var newCat = new ProductCategory
                        {
                            CategoryID = newCatId,
                            CategoryName = typedCategory
                        };
                        DataProvider.Ins.DB.ProductCategories.Add(newCat);
                        DataProvider.Ins.DB.SaveChanges();

                        // update local list & chosen id
                        LoadCategories(); // reload to keep view consistent
                        chosenCategoryID = newCatId;
                    }
                }

                // If user selected existing category, ensure chosenCategoryID is set
                if (string.IsNullOrWhiteSpace(chosenCategoryID) && !string.IsNullOrWhiteSpace(SelectedCategoryID))
                    chosenCategoryID = SelectedCategoryID;

                // Tạo sản phẩm mới
                var newProduct = new Product
                {
                    ProductID = newProductID,
                    ProductName = ProductName.Trim(),
                    Unit = newUnit,
                    Price = Price.Value,
                    StockQuantity = StockQuantity.Value,
                    ImagePath = dbImagePath,
                    CategoryID = chosenCategoryID,
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
                nameof(SelectedCategoryID),
                nameof(SelectedCategoryText)
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

        // new helper to generate category id
        private string GenerateCategoryID()
        {
            try
            {
                var ids = DataProvider.Ins.DB.ProductCategories
                    .Select(x => x.CategoryID)
                    .ToList();

                int max = 0;
                foreach (var id in ids)
                {
                    // extract trailing digits
                    var digits = new string(id?.SkipWhile(c => !char.IsDigit(c)).ToArray());
                    if (int.TryParse(digits, out int n))
                    {
                        if (n > max) max = n;
                    }
                }

                return "C" + (max + 1).ToString("D3");
            }
            catch
            {
                return "C001";
            }
        }
    }
}