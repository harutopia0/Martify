using Martify.Models;
using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class InventoryAlertWindowVM : BaseVM
    {
        private InventoryAlertType _alertType;
        public InventoryAlertType AlertType
        {
            get => _alertType;
            set
            {
                _alertType = value;
                OnPropertyChanged();
            }
        }

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

        private ObservableCollection<ProductCategory> _categories;
        public ObservableCollection<ProductCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _unitList;
        public ObservableCollection<string> UnitList
        {
            get => _unitList;
            set
            {
                _unitList = value;
                OnPropertyChanged();
            }
        }

        private List<SelectableProduct> _allProducts;

        private ObservableCollection<SelectableProduct> _products;
        public ObservableCollection<SelectableProduct> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
                CalculateStatistics();
            }
        }

        private bool _isAllSelected;
        private bool _isUpdatingSelection = false;
        
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged();
                    
                    // When the header checkbox is clicked, update all product selections
                    if (!_isUpdatingSelection && Products != null)
                    {
                        _isUpdatingSelection = true;
                        foreach (var product in Products)
                        {
                            product.IsSelected = value;
                        }
                        UpdateSelectedCount();
                        _isUpdatingSelection = false;
                    }
                }
            }
        }

        private int _selectedCount;
        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                _selectedCount = value;
                OnPropertyChanged();
            }
        }

        private int _totalStockQuantity;
        public int TotalStockQuantity
        {
            get => _totalStockQuantity;
            set
            {
                _totalStockQuantity = value;
                OnPropertyChanged();
            }
        }

        private decimal _totalValue;
        public decimal TotalValue
        {
            get => _totalValue;
            set
            {
                _totalValue = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectAllCommand { get; set; }
        public ICommand RestockCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }
        public ICommand DeleteProductCommand { get; set; }

        public InventoryAlertWindowVM(InventoryAlertType alertType)
        {
            AlertType = alertType;
            Categories = new ObservableCollection<ProductCategory>();
            UnitList = new ObservableCollection<string>();
            _allProducts = new List<SelectableProduct>();
            InitializeCommands();
            LoadCategories();
            LoadUnits();
            LoadProducts();
        }

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand<object>((p) => true, (p) => SelectAllProducts());
            RestockCommand = new RelayCommand<object>((p) => HasSelectedProducts(), (p) => RestockSelectedProducts());
            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) => ClearFilters());
            DeleteProductCommand = new RelayCommand<SelectableProduct>((p) => p != null, (p) => DeleteProduct(p));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedUnit = null;
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

        private void LoadUnits()
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

        private bool HasSelectedProducts()
        {
            return Products != null && Products.Any(p => p.IsSelected);
        }

        private void LoadProducts()
        {
            try
            {
                var query = DataProvider.Ins.DB.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                if (AlertType == InventoryAlertType.LowStock)
                {
                    // Low stock: StockQuantity > 0 AND StockQuantity <= 10
                    const int MIN_STOCK_THRESHOLD = 10;
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= MIN_STOCK_THRESHOLD);
                }
                else if (AlertType == InventoryAlertType.OutOfStock)
                {
                    // Out of stock: StockQuantity = 0
                    query = query.Where(p => p.StockQuantity == 0);
                }

                var result = query
                    .OrderBy(p => p.StockQuantity)
                    .ThenBy(p => p.ProductName)
                    .ToList();

                // Store in master list
                _allProducts = result.Select(p => new SelectableProduct(p)).ToList();

                // Subscribe to selection changes for all products
                foreach (var selectableProduct in _allProducts)
                {
                    selectableProduct.PropertyChanged += SelectableProduct_PropertyChanged;
                }

                // Apply filters
                FilterProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi tải danh sách sản phẩm: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FilterProducts()
        {
            if (_allProducts == null)
                return;

            var filtered = _allProducts.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLower();
                filtered = filtered.Where(sp =>
                    sp.Product.ProductID.ToLower().Contains(search) ||
                    sp.Product.ProductName.ToLower().Contains(search) ||
                    (sp.Product.Unit != null && sp.Product.Unit.ToLower().Contains(search)) ||
                    (sp.Product.Category != null && sp.Product.Category.CategoryName.ToLower().Contains(search)));
            }

            // Filter by category
            if (SelectedCategory != null)
            {
                filtered = filtered.Where(sp => sp.Product.CategoryID == SelectedCategory.CategoryID);
            }

            // Filter by unit
            if (!string.IsNullOrWhiteSpace(SelectedUnit))
            {
                var unitLower = SelectedUnit.Trim().ToLower();
                filtered = filtered.Where(sp => sp.Product.Unit != null && sp.Product.Unit.ToLower() == unitLower);
            }

            Products = new ObservableCollection<SelectableProduct>(filtered);
            UpdateSelectedCount();
            UpdateSelectAllState();
        }

        private void SelectableProduct_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableProduct.IsSelected))
            {
                UpdateSelectedCount();
                UpdateSelectAllState();
            }
        }

        private void UpdateSelectedCount()
        {
            if (Products != null)
            {
                SelectedCount = Products.Count(p => p.IsSelected);
            }
        }

        private void UpdateSelectAllState()
        {
            if (!_isUpdatingSelection && Products != null && Products.Count > 0)
            {
                _isUpdatingSelection = true;
                _isAllSelected = Products.All(p => p.IsSelected);
                OnPropertyChanged(nameof(IsAllSelected));
                _isUpdatingSelection = false;
            }
        }

        private void SelectAllProducts()
        {
            if (Products == null || Products.Count == 0)
                return;

            bool newState = !IsAllSelected;
            foreach (var product in Products)
            {
                product.IsSelected = newState;
            }
            IsAllSelected = newState;
        }

        private const int DEFAULT_RESTOCK_QUANTITY = 50;

        private void RestockSelectedProducts()
        {
            var selectedProducts = Products?.Where(p => p.IsSelected).Select(sp => sp.Product).ToList();
            
            if (selectedProducts == null || selectedProducts.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn ít nhất một sản phẩm để nhập hàng.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Create ViewModel with pre-selected products and default quantity
            var importVM = new ImportProductsVM(selectedProducts, DEFAULT_RESTOCK_QUANTITY);
            
            // Open ImportProducts window with pre-loaded products
            var importWindow = new ImportProducts(importVM)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };
            
            importWindow.ShowDialog();

            // Refresh products after import to reflect updated stock quantities
            LoadProducts();
        }

        private void DeleteProduct(SelectableProduct selectableProduct)
        {
            if (selectableProduct == null) return;

            var product = selectableProduct.Product;

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

                    // Unsubscribe from property changed event
                    selectableProduct.PropertyChanged -= SelectableProduct_PropertyChanged;

                    // Remove from database
                    var productInDb = DataProvider.Ins.DB.Products.Find(product.ProductID);
                    if (productInDb != null)
                    {
                        DataProvider.Ins.DB.Products.Remove(productInDb);
                        DataProvider.Ins.DB.SaveChanges();
                    }

                    // Remove from master list and filtered list
                    _allProducts.Remove(selectableProduct);
                    Products.Remove(selectableProduct);

                    MessageBox.Show("Đã xóa sản phẩm!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    CalculateStatistics();
                    UpdateSelectedCount();
                    UpdateSelectAllState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateStatistics()
        {
            if (Products == null || Products.Count == 0)
            {
                TotalStockQuantity = 0;
                TotalValue = 0;
                return;
            }

            TotalStockQuantity = Products.Sum(p => p.Product.StockQuantity);
            TotalValue = Products.Sum(p => p.Product.StockQuantity * p.Product.Price);
        }
    }
}
