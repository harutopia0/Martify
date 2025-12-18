using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class ProductsVM : BaseVM
    {
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterProductsInMemory();
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
                FilterProductsInMemory();
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
                FilterProductsInMemory();
            }
        }

        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(); }
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
                FilterProductsInMemory();
            }
        }

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

        private Product _selectedDetailProduct;
        public Product SelectedDetailProduct
        {
            get => _selectedDetailProduct;
            set
            {
                _selectedDetailProduct = value;
                OnPropertyChanged();
            }
        }

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

        private string _editImagePath;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); }
        }

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set { _isDetailsPanelOpen = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand AddProductCommand { get; set; }
        public ICommand DeleteProductCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand ImportProductCommand { get; set; }
        public ICommand SaveChangesCommand { get; set; }
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }

        // Cache toàn bộ products trong memory
        private List<Product> _allProductsList;

        public ProductsVM()
        {
            // Initialize collections
            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<ProductCategory>();
            UnitList = new ObservableCollection<string>();
            _allProductsList = new List<Product>();
            InventoryAlertFilter = InventoryAlertType.None;

            // Initialize commands
            AddProductCommand = new RelayCommand<object>((p) => true, (p) => AddProduct());
            DeleteProductCommand = new RelayCommand<Product>((p) => p != null, (p) => DeleteProduct(p));
            RefreshCommand = new RelayCommand<object>((p) => true, (p) => LoadData());
            ImportProductCommand = new RelayCommand<object>((p) => true, (p) => ImportProducts());
            SelectImageCommand = new RelayCommand<object>((p) => true, (p) => { });

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
                    IsDetailsPanelOpen = false;
                    SelectedDetailProduct = null;
                });

            SaveChangesCommand = new RelayCommand<object>(
                (p) => IsModified,
                async (p) => await SaveChangesAsync());

            // Load data
            LoadData();
        }

        private void LoadData()
        {
            LoadCategories();
            LoadUnit();
            LoadProducts();
        }

        private bool IsValid()
        {
            if (SelectedDetailProduct == null) return false;
            if (string.IsNullOrWhiteSpace(EditProductName)) return false;
            if (string.IsNullOrWhiteSpace(EditUnit)) return false;
            if (EditPrice <= 0) return false;
            if (EditStockQuantity < 0) return false;
            if (string.IsNullOrWhiteSpace(EditCategoryID)) return false;
            return true;
        }

        private async Task SaveChangesAsync()
        {
            if (SelectedDetailProduct == null) return;

            if (!IsValid())
            {
                SaveMessage = "Vui lòng kiểm tra lại thông tin!";
                await Task.Delay(3000);
                if (SaveMessage == "Vui lòng kiểm tra lại thông tin!")
                    SaveMessage = "";
                return;
            }

            try
            {
                var productInDb = DataProvider.Ins.DB.Products
                    .FirstOrDefault(x => x.ProductID == SelectedDetailProduct.ProductID);

                if (productInDb != null)
                {
                    productInDb.ProductName = EditProductName?.Trim();
                    productInDb.Price = EditPrice;
                    productInDb.StockQuantity = EditStockQuantity;
                    productInDb.Unit = EditUnit?.Trim();
                    productInDb.CategoryID = EditCategoryID;

                    DataProvider.Ins.DB.SaveChanges();

                    SaveMessage = "Đã lưu thay đổi!";
                    IsModified = false;

                    LoadProducts();

                    SelectedDetailProduct = Products.FirstOrDefault(
                        x => x.ProductID == productInDb.ProductID);

                    await Task.Delay(3000);
                    if (SaveMessage == "Đã lưu thay đổi!")
                        SaveMessage = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveMessage = "Lỗi khi lưu!";
            }
        }

        private void OpenDetails(Product product)
        {
            if (product == null) return;

            if (IsDetailsPanelOpen && SelectedDetailProduct != null && SelectedDetailProduct.ProductID == product.ProductID)
            {
                IsDetailsPanelOpen = false;
                return;
            }

            EditProductName = product.ProductName;
            EditPrice = product.Price;
            EditStockQuantity = product.StockQuantity;
            EditUnit = product.Unit;
            EditCategoryID = product.CategoryID;
            EditImagePath = product.ImagePath;

            var lastImport = product.ImportReceiptDetails?
                            .OrderByDescending(d => d.ImportReceipt.ImportDate)
                            .FirstOrDefault();

            SupplierName = lastImport?.ImportReceipt?.Supplier?.SupplierName ?? "Chưa có đợt nhập hàng";

            SelectedDetailProduct = product;

            IsModified = false;
            SaveMessage = string.Empty;
            IsDetailsPanelOpen = true;
        }

        public void SetInventoryAlertFilter(InventoryAlertType alertType)
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedUnit = null;
            InventoryAlertFilter = alertType;
        }

        private void LoadCategories()
        {
            try
            {
                var list = DataProvider.Ins.DB.ProductCategories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToList();
                Categories = new ObservableCollection<ProductCategory>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUnit()
        {
            try
            {
                var list = DataProvider.Ins.DB.Products
                    .AsNoTracking()
                    .Select(p => p.Unit)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(unit => unit)
                    .ToList();
                UnitList = new ObservableCollection<string>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải đơn vị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var productList = DataProvider.Ins.DB.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ImportReceiptDetails).ThenInclude(ir => ir.ImportReceipt).ThenInclude(sp => sp.Supplier)
                    .OrderBy(p => p.ProductID)
                    .ToList();

                _allProductsList = productList;
                FilterProductsInMemory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterProductsInMemory()
        {
            try
            {
                var filtered = _allProductsList.AsEnumerable();

                if (SelectedCategory != null)
                    filtered = filtered.Where(p => p.CategoryID == SelectedCategory.CategoryID);

                if (!string.IsNullOrWhiteSpace(SelectedUnit))
                {
                    var unitLower = SelectedUnit.Trim().ToLower();
                    filtered = filtered.Where(p => p.Unit != null && p.Unit.ToLower() == unitLower);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = ConvertToUnSign(SearchText).ToLower();
                    filtered = filtered.Where(p =>
                        ConvertToUnSign(p.ProductID).ToLower().Contains(search) ||
                        ConvertToUnSign(p.ProductName).ToLower().Contains(search) ||
                        (p.Unit != null && ConvertToUnSign(p.Unit).ToLower().Contains(search)) ||
                        (p.Category != null && ConvertToUnSign(p.Category.CategoryName).ToLower().Contains(search)));
                }

                if (InventoryAlertFilter != InventoryAlertType.None)
                {
                    if (InventoryAlertFilter == InventoryAlertType.LowStock)
                    {
                        const int MIN_STOCK_THRESHOLD = 10;
                        filtered = filtered.Where(p => p.StockQuantity > 0 && p.StockQuantity <= MIN_STOCK_THRESHOLD);
                    }
                    else if (InventoryAlertFilter == InventoryAlertType.OutOfStock)
                    {
                        filtered = filtered.Where(p => p.StockQuantity == 0);
                    }
                }

                var result = filtered.OrderBy(p => p.ProductID).ToList();
                Products = new ObservableCollection<Product>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lọc sản phẩm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ConvertToUnSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(System.Text.NormalizationForm.FormD);

            return regex
                .Replace(temp, string.Empty)
                .Replace('\u0111', 'd')
                .Replace('\u0110', 'D');
        }

        private void AddProduct()
        {
            try
            {
                var addWindow = new AddProduct();
                addWindow.ShowDialog();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportProducts()
        {
            try
            {
                var importWindow = new ImportProducts();
                importWindow.ShowDialog();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteProduct(Product product)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Xóa sản phẩm:\n\nMã: {product.ProductID}\nTên: {product.ProductName}?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var isUsedInOrders = DataProvider.Ins.DB.InvoiceDetails
                        .Any(od => od.ProductID == product.ProductID);

                    if (isUsedInOrders)
                    {
                        MessageBox.Show("Không thể xóa sản phẩm đã có trong đơn hàng.",
                            "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DataProvider.Ins.DB.Products.Remove(product);
                    DataProvider.Ins.DB.SaveChanges();

                    MessageBox.Show("Đã xóa sản phẩm!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}