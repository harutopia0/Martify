using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
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

        // --- DETAIL PANEL DATA & CONTROL ---
        private Product _selectedDetailProduct;
        public Product SelectedDetailProduct
        {
            get => _selectedDetailProduct;
            set { _selectedDetailProduct = value; OnPropertyChanged(); }
        }

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set { _isDetailsPanelOpen = value; OnPropertyChanged(); }
        }

        // --- COMMANDS ---
        public ICommand AddProductCommand { get; set; }
        public ICommand EditProductCommand { get; set; }
        public ICommand DeleteProductCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        // Commands used by XAML
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }

        public ProductsVM()
        {
            LoadCategories();
            LoadUnit();
            LoadProducts();

            AddProductCommand = new RelayCommand<object>((p) => true, (p) => AddProduct());
            EditProductCommand = new RelayCommand<Product>((p) => p != null, (p) => EditProduct(p));
            DeleteProductCommand = new RelayCommand<Product>((p) => p != null, (p) => DeleteProduct(p));
            RefreshCommand = new RelayCommand<object>((p) => true, (p) => { LoadCategories(); LoadProducts(); });

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
                    FilterProducts();
                    // also close details panel
                    IsDetailsPanelOpen = false;
                    SelectedDetailProduct = null;
                });
        }

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

        private void OpenDetails(Product product)
        {
            if (product == null) return;

            // Toggle behavior: if same product double-clicked, toggle panel; otherwise open for the new product
            if (SelectedDetailProduct != null && SelectedDetailProduct.ProductID == product.ProductID)
            {
                IsDetailsPanelOpen = !IsDetailsPanelOpen;
            }
            else
            {
                SelectedDetailProduct = product;
                IsDetailsPanelOpen = true;
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

        private void EditProduct(Product product)
        {
            try
            {
                MessageBox.Show($"Chức năng chỉnh sửa sản phẩm: {product.ProductName}\nTính năng đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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