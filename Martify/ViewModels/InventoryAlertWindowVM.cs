using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public InventoryAlertWindowVM(InventoryAlertType alertType)
        {
            AlertType = alertType;
            InitializeCommands();
            LoadProducts();
        }

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand<object>((p) => true, (p) => SelectAllProducts());
            RestockCommand = new RelayCommand<object>((p) => HasSelectedProducts(), (p) => RestockSelectedProducts());
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

                Products = new ObservableCollection<SelectableProduct>(
                    result.Select(p => new SelectableProduct(p))
                );

                // Subscribe to selection changes
                foreach (var selectableProduct in Products)
                {
                    selectableProduct.PropertyChanged += SelectableProduct_PropertyChanged;
                }

                UpdateSelectedCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"L?i khi t?i danh sách s?n ph?m: {ex.Message}",
                    "L?i",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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

        private void RestockSelectedProducts()
        {
            var selectedProducts = Products?.Where(p => p.IsSelected).Select(sp => sp.Product).ToList();
            
            if (selectedProducts == null || selectedProducts.Count == 0)
            {
                MessageBox.Show(
                    "Vui l?ng ch?n ít nh?t m?t s?n ph?m ð? nh?p hàng.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // TODO: Open import receipt dialog with selected products
            // For now, show a message with the count
            MessageBox.Show(
                $"Ð? ch?n {selectedProducts.Count} s?n ph?m ð? nh?p hàng.\n" +
                $"S?n ph?m: {string.Join(", ", selectedProducts.Select(p => p.ProductName))}",
                "Nh?p hàng",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
