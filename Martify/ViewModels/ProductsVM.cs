using Martify.Helpers;
using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Cần thiết cho Task.Delay
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    // Giả định: Enum này nằm trong một file riêng (hoặc trong Models)

    public class ProductsVM : BaseVM
    {
        // --- FILTERS ---
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        private ProductCategory _selectedCategory;
        public ProductCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        private string _selectedUnit;
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        private string _saveMessage;
        public string SaveMessage
        {
            get => _saveMessage;
            set { _saveMessage = value; OnPropertyChanged(); }
        }

        private InventoryAlertType _inventoryAlertFilter;
        public InventoryAlertType InventoryAlertFilter
        {
            get => _inventoryAlertFilter;
            set
            {
                _inventoryAlertFilter = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        // --- COMBO SOURCE ---
        private ObservableCollection<ProductCategory> _categories;
        public ObservableCollection<ProductCategory> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _unitList;
        public ObservableCollection<string> UnitList
        {
            get => _unitList;
            set { _unitList = value; OnPropertyChanged(); }
        }

        // --- PRODUCTS LIST ---
        private ObservableCollection<Product> _products;
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        // --- DETAIL PANEL DATA & CONTROL (Thuộc tính dùng để chỉnh sửa sản phẩm) ---
        private Product _selectedDetailProduct;
        public Product SelectedDetailProduct
        {
            get => _selectedDetailProduct;
            set
            {
                _selectedDetailProduct = value;
                OnPropertyChanged();
                // Logic cập nhật các thuộc tính Edit đã được chuyển sang OpenDetailsCommand/OpenDetails method
            }
        }

        // --- CÁC THUỘC TÍNH DÙNG CHO EDITING (Để kiểm tra Validation) ---
        private string _editProductName;
        public string EditProductName
        {
            get => _editProductName;
            set { _editProductName = value; OnPropertyChanged(); IsModified = true; }
        }

        private decimal _editPrice;
        public decimal EditPrice
        {
            get => _editPrice;
            set { _editPrice = value; OnPropertyChanged(); IsModified = true; }
        }

        private int _editStockQuantity;
        public int EditStockQuantity
        {
            get => _editStockQuantity;
            set { _editStockQuantity = value; OnPropertyChanged(); IsModified = true; }
        }

        private string _editUnit;
        public string EditUnit
        {
            get => _editUnit;
            set { _editUnit = value; OnPropertyChanged(); IsModified = true; }
        }

        private string _editCategoryID;
        public string EditCategoryID
        {
            get => _editCategoryID;
            set { _editCategoryID = value; OnPropertyChanged(); IsModified = true; }
        }

        private string _sourceImageFile; // Giữ lại nếu bạn có thể cần nó cho sản phẩm trong tương lai

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        // --- KẾT THÚC THUỘC TÍNH EDITING ---

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set { _isDetailsPanelOpen = value; OnPropertyChanged(); }
        }

        // --- COMMANDS ---
        public ICommand AddProductCommand { get; set; }
        public ICommand DeleteProductCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ImportProductCommand { get; set; }
        public ICommand SaveChangesCommand { get; set; }
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }


        // Logic Validate Sản phẩm
        private bool IsValid()
        {
            if (SelectedDetailProduct == null) return false;

            string id = SelectedDetailProduct.ProductID;

            // 1. Kiểm tra Tên sản phẩm (Có kiểm tra trùng, bỏ qua ID hiện tại)
            if (ProductValidator.CheckProductName(EditProductName, id) != null) return false;

            // 2. Kiểm tra Đơn vị tính
            if (ProductValidator.CheckUnit(EditUnit) != null) return false;

            // 3. Kiểm tra Giá bán
            // Lưu ý: EditPrice là decimal, CheckPrice nhận decimal?. Cần đảm bảo giá trị hợp lệ.
            if (ProductValidator.CheckPrice(EditPrice) != null) return false;

            // 4. Kiểm tra Tồn kho
            // Lưu ý: EditStockQuantity là int, CheckStockQuantity nhận int?. Cần đảm bảo giá trị hợp lệ.
            if (ProductValidator.CheckStockQuantity(EditStockQuantity) != null) return false;

            // 5. Kiểm tra Danh mục
            if (ProductValidator.CheckCategoryID(EditCategoryID) != null) return false;

            return true;
        }

        public ProductsVM()
        {
            LoadCategories();
            LoadUnit();
            LoadProducts();
            InventoryAlertFilter = InventoryAlertType.None; // Thiết lập giá trị mặc định

            AddProductCommand = new RelayCommand<object>((p) => true, (p) => AddProduct());
            DeleteProductCommand = new RelayCommand<Product>((p) => p != null, (p) => DeleteProduct(p));
            RefreshCommand = new RelayCommand<object>((p) => true, (p) => { LoadCategories(); LoadProducts(); });
            ImportProductCommand = new RelayCommand<object>((p) => true, (p) => ImportProducts());

            // Đã sửa lại OpenDetailsCommand để gọi hàm OpenDetails
            OpenDetailsCommand = new RelayCommand<Product>(
                (p) => p != null,
                (p) => OpenDetails(p));

            ClearFilterCommand = new RelayCommand<object>(
                (p) => true,
                (p) =>
                {
                    SearchText = string.Empty;
                    SelectedCategory = null;
                    SelectedUnit = null;
                    InventoryAlertFilter = InventoryAlertType.None;
                    FilterProducts();
                    IsDetailsPanelOpen = false;
                    SelectedDetailProduct = null;
                });

            // Logic lưu dữ liệu Sản phẩm
            SaveChangesCommand = new RelayCommand<object>((p) => IsModified, async (p) =>
            {
                if (SelectedDetailProduct == null) return;

                // --- Validation ---
                if (!IsValid())
                {
                    SaveMessage = "Vui lòng kiểm tra lại thông tin lỗi!";
                    await Task.Delay(3000);
                    if (SaveMessage == "Vui lòng kiểm tra lại thông tin lỗi!") SaveMessage = "";
                    return;
                }

                // --- Save Logic ---
                var productInDb = DataProvider.Ins.DB.Products
                    .FirstOrDefault(x => x.ProductID == SelectedDetailProduct.ProductID);

                if (productInDb != null)
                {
                    try
                    {
                        // Cập nhật các trường dữ liệu của sản phẩm từ thuộc tính Edit
                        productInDb.ProductName = EditProductName;
                        productInDb.Price = EditPrice;
                        productInDb.StockQuantity = EditStockQuantity;
                        productInDb.Unit = EditUnit;
                        productInDb.CategoryID = EditCategoryID;
                        // ...

                        DataProvider.Ins.DB.SaveChanges();

                        SaveMessage = "Đã lưu thay đổi sản phẩm thành công!";
                        IsModified = false;

                        // Tải lại danh sách
                        LoadProducts();

                        // Cập nhật SelectedDetailProduct (để refresh hiển thị)
                        SelectedDetailProduct = Products.FirstOrDefault(
                            x => x.ProductID == productInDb.ProductID);

                        await Task.Delay(3000);
                        if (SaveMessage == "Đã lưu thay đổi sản phẩm thành công!") SaveMessage = "";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi lưu sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        SaveMessage = "Lỗi khi lưu dữ liệu!";
                    }
                }
            });
        }

        // Đã sửa logic này để đảm bảo dữ liệu được sao chép chính xác
        private void OpenDetails(Product product)
        {
            if (product == null) return;

            // 1. Cập nhật SelectedProduct (cho list selection)
            SelectedProduct = product;

            // 2. Nếu là sản phẩm cũ được click lần nữa, toggle panel
            if (SelectedDetailProduct != null && SelectedDetailProduct.ProductID == product.ProductID)
            {
                IsDetailsPanelOpen = !IsDetailsPanelOpen;
            }
            else
            {
                // 3. Sao chép dữ liệu từ Model sang các thuộc tính Edit
                EditProductName = product.ProductName;
                EditPrice = product.Price;
                EditStockQuantity = product.StockQuantity;
                EditUnit = product.Unit;
                EditCategoryID = product.CategoryID;

                // 4. Đặt SelectedDetailProduct để kích hoạt binding và panel
                SelectedDetailProduct = product;

                // 5. Reset trạng thái
                IsModified = false;
                SaveMessage = string.Empty;
                IsDetailsPanelOpen = true;
            }
        }

        // ... (Các hàm LoadCategories, LoadUnit, LoadProducts, FilterProducts, AddProduct, ImportProducts, DeleteProduct) ...

        /// <summary>
        /// Đặt bộ lọc cảnh báo tồn kho và tải lại sản phẩm
        /// </summary>
        public void SetInventoryAlertFilter(InventoryAlertType alertType)
        {
            // Clear other filters
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedUnit = null;

            // Set alert filter
            InventoryAlertFilter = alertType;

            // Reload products with alert filter
            FilterProducts();
        }

        private void LoadCategories()
        {
            try
            {
                var list = DataProvider.Ins.DB.ProductCategories
                    .OrderBy(c => c.CategoryName)
                    .ToList();
                Categories = new ObservableCollection<ProductCategory>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUnit()
        {
            try
            {
                var list = DataProvider.Ins.DB.Products
                    .Select(p => p.Unit)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(unit => unit)
                    .ToList();
                UnitList = new ObservableCollection<string>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách đơn vị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var productList = DataProvider.Ins.DB.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.ProductID)
                    .ToList();

                Products = new ObservableCollection<Product>(productList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterProducts()
        {
            try
            {
                var query = DataProvider.Ins.DB.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // Filter by category
                if (SelectedCategory != null)
                    query = query.Where(p => p.CategoryID == SelectedCategory.CategoryID);

                // Filter by unit
                if (!string.IsNullOrWhiteSpace(SelectedUnit))
                {
                    var unitLower = SelectedUnit.Trim().ToLower();
                    query = query.Where(p => p.Unit != null && p.Unit.ToLower() == unitLower);
                }

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.Trim().ToLower();
                    query = query.Where(p =>
                        p.ProductID.ToLower().Contains(search) ||
                        p.ProductName.ToLower().Contains(search) ||
                        (p.Unit != null && p.Unit.ToLower().Contains(search)) ||
                        (p.Category != null && p.Category.CategoryName.ToLower().Contains(search)));
                }

                // Filter by inventory alert type
                if (InventoryAlertFilter != InventoryAlertType.None)
                {
                    if (InventoryAlertFilter == InventoryAlertType.LowStock)
                    {
                        // Low stock: StockQuantity > 0 AND StockQuantity <= 10
                        const int MIN_STOCK_THRESHOLD = 10;
                        query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= MIN_STOCK_THRESHOLD);
                    }
                    else if (InventoryAlertFilter == InventoryAlertType.OutOfStock)
                    {
                        // Out of stock: StockQuantity = 0
                        query = query.Where(p => p.StockQuantity == 0);
                    }
                }

                var result = query.OrderBy(p => p.ProductID).ToList();
                Products = new ObservableCollection<Product>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lọc sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddProduct()
        {
            try
            {
                var addWindow = new AddProduct();
                addWindow.ShowDialog();
                LoadCategories();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ thêm sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportProducts()
        {
            try
            {
                var importWindow = new ImportProducts();
                importWindow.ShowDialog();

                // Reload products after import
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ nhập hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProduct(Product product)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa sản phẩm:\n\nMã: {product.ProductID}\nTên: {product.ProductName}\n\nHành động này không thể hoàn tác!",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var isUsedInOrders = DataProvider.Ins.DB.InvoiceDetails.Any(od => od.ProductID == product.ProductID);
                    if (isUsedInOrders)
                    {
                        MessageBox.Show("Không thể xóa sản phẩm này vì đã được sử dụng trong các đơn hàng.\nBạn có thể ngừng kinh doanh sản phẩm này thay vì xóa.", "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DataProvider.Ins.DB.Products.Remove(product);
                    DataProvider.Ins.DB.SaveChanges();

                    MessageBox.Show("Đã xóa sản phẩm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}