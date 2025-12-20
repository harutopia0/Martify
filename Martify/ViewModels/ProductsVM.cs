using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
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
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }

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
                });

            // Load data
            LoadData();
        }

        private void LoadData()
        {
            LoadCategories();
            LoadUnit();
            LoadProducts();
        }

        private void OpenDetails(Product product)
        {
            if (product == null) return;

            try
            {
                // Load full product with related data
                var fullProduct = DataProvider.Ins.DB.Products
                    .Include(p => p.Category)
                    .Include(p => p.ImportReceiptDetails)
                        .ThenInclude(ir => ir.ImportReceipt)
                        .ThenInclude(sp => sp.Supplier)
                    .FirstOrDefault(p => p.ProductID == product.ProductID);

                if (fullProduct != null)
                {
                    var detailVM = new ProductDetailVM(fullProduct);

                    // Set callback to refresh data after save
                    detailVM.OnSaveCompleted = () => LoadProducts();

                    var detailWindow = new ProductDetail(detailVM);
                    detailWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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