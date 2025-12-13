using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

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

        private ObservableCollection<Product> _products;
        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
                CalculateStatistics();
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

        public InventoryAlertWindowVM(InventoryAlertType alertType)
        {
            AlertType = alertType;
            LoadProducts();
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

                Products = new ObservableCollection<Product>(result);
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

        private void CalculateStatistics()
        {
            if (Products == null || Products.Count == 0)
            {
                TotalStockQuantity = 0;
                TotalValue = 0;
                return;
            }

            TotalStockQuantity = Products.Sum(p => p.StockQuantity);
            TotalValue = Products.Sum(p => p.StockQuantity * p.Price);
        }
    }
}
