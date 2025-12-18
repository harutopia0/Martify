using Martify.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class ImportProductsVM : BaseVM, IDataErrorInfo
    {
        // =================================================================================================
        // PROPERTIES
        // =================================================================================================

        private ObservableCollection<ImportItem> _importItems;
        public ObservableCollection<ImportItem> ImportItems
        {
            get => _importItems;
            set { _importItems = value; OnPropertyChanged(); }
        }

        private ImportItem _selectedImportItem;
        public ImportItem SelectedImportItem
        {
            get => _selectedImportItem;
            set { _selectedImportItem = value; OnPropertyChanged(); }
        }

        private List<Product> _productList;
        public List<Product> ProductList
        {
            get => _productList;
            set { _productList = value; OnPropertyChanged(); }
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

        private ObservableCollection<Product> _filteredProducts;
        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set { _filteredProducts = value; OnPropertyChanged(); }
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        // =================================================================================================
        // VALIDATION
        // =================================================================================================

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(SelectedSupplierID):
                        if (string.IsNullOrWhiteSpace(SelectedSupplierID))
                            error = "Vui lòng chọn nhà cung cấp.";
                        break;
                }
                return error;
            }
        }

        // =================================================================================================
        // COMMANDS
        // =================================================================================================

        public ICommand AddProductCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
        public ICommand SaveImportCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand IncreaseQuantityCommand { get; set; }
        public ICommand DecreaseQuantityCommand { get; set; }

        // =================================================================================================
        // CONSTRUCTOR
        // =================================================================================================

        public ImportProductsVM()
        {
            ImportItems = new ObservableCollection<ImportItem>();
            LoadProducts();
            LoadSuppliers();
            InitializeCommands();
        }

        /// <summary>
        /// Constructor for pre-loading products with a default quantity (used for restocking from inventory alerts)
        /// </summary>
        /// <param name="products">Products to pre-load</param>
        /// <param name="defaultQuantity">Default quantity for each product</param>
        public ImportProductsVM(IEnumerable<Product> products, int defaultQuantity = 50)
        {
            ImportItems = new ObservableCollection<ImportItem>();
            LoadProducts();
            LoadSuppliers();
            InitializeCommands();

            // Pre-load selected products with default quantity
            if (products != null)
            {
                foreach (var product in products)
                {
                    var importItem = new ImportItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        Unit = product.Unit,
                        UnitPrice = product.Price,
                        Quantity = defaultQuantity
                    };

                    importItem.PropertyChanged += ImportItem_PropertyChanged;
                    ImportItems.Add(importItem);
                }

                CalculateTotal();
            }
        }

        private void InitializeCommands()
        {
            AddProductCommand = new RelayCommand<Product>(
                (p) => p != null,
                (p) => AddProductToImport(p));

            RemoveItemCommand = new RelayCommand<ImportItem>(
                (p) => p != null,
                (p) => RemoveItem(p));

            SaveImportCommand = new RelayCommand<Window>(
                (p) => CanSave(),
                (p) => SaveImport(p));

            CloseCommand = new RelayCommand<Window>(
                (p) => true,
                (p) => p?.Close());

            IncreaseQuantityCommand = new RelayCommand<ImportItem>(
                (p) => p != null,
                (p) => p.Quantity++);

            DecreaseQuantityCommand = new RelayCommand<ImportItem>(
                (p) => p != null && p.Quantity > 1,
                (p) => p.Quantity--);

            // Subscribe to collection changes
            ImportItems.CollectionChanged += (s, e) => CalculateTotal();
        }

        // =================================================================================================
        // METHODS
        // =================================================================================================

        private void LoadProducts()
        {
            try
            {
                ProductList = DataProvider.Ins.DB.Products
                    .OrderBy(p => p.ProductName)
                    .ToList();

                FilteredProducts = new ObservableCollection<Product>(ProductList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sản phẩm: {ex.Message}",
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

        private void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredProducts = new ObservableCollection<Product>(ProductList);
                return;
            }

            var search = SearchText.Trim().ToLower();
            var filtered = ProductList
                .Where(p =>
                    p.ProductID.ToLower().Contains(search) ||
                    p.ProductName.ToLower().Contains(search))
                .ToList();

            FilteredProducts = new ObservableCollection<Product>(filtered);
        }

        private void AddProductToImport(Product product)
        {
            if (product == null) return;

            // Check if product already exists in import list
            var existing = ImportItems.FirstOrDefault(i => i.ProductID == product.ProductID);
            if (existing != null)
            {
                existing.Quantity++;
                existing.RecalculateSubtotal();
                CalculateTotal();
                return;
            }

            var importItem = new ImportItem
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                UnitPrice = product.Price,
                Quantity = 1
            };

            importItem.PropertyChanged += ImportItem_PropertyChanged;
            ImportItems.Add(importItem);
        }

        private void ImportItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportItem.Subtotal))
            {
                CalculateTotal();
            }
        }

        private void RemoveItem(ImportItem item)
        {
            if (item != null)
            {
                item.PropertyChanged -= ImportItem_PropertyChanged;
                ImportItems.Remove(item);
            }
        }

        private void CalculateTotal()
        {
            TotalAmount = ImportItems.Sum(i => i.Subtotal);
        }

        private bool CanSave()
        {
            return ImportItems.Any() && !string.IsNullOrWhiteSpace(SelectedSupplierID);
        }

        private void SaveImport(Window window)
        {
            if (!CanSave())
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp và thêm ít nhất một sản phẩm.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create import receipt
                string receiptID = GenerateReceiptID();
                var receipt = new ImportReceipt
                {
                    ReceiptID = receiptID,
                    SupplierID = SelectedSupplierID,
                    ImportDate = DateTime.Now,
                    TotalAmount = TotalAmount
                };

                DataProvider.Ins.DB.ImportReceipts.Add(receipt);

                // Update product stock quantities and create receipt details
                foreach (var item in ImportItems)
                {
                    var product = DataProvider.Ins.DB.Products
                        .FirstOrDefault(p => p.ProductID == item.ProductID);

                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;

                        // Create receipt detail
                        var detail = new ImportReceiptDetail
                        {
                            ReceiptID = receiptID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };

                        DataProvider.Ins.DB.ImportReceiptDetails.Add(detail);
                    }
                }

                DataProvider.Ins.DB.SaveChanges();

                MessageBox.Show(
                    $"Nhập hàng thành công!\n\n" +
                    $"Mã phiếu: {receiptID}\n" +
                    $"Tổng tiền: {TotalAmount:N0} VNĐ\n" +
                    $"Số sản phẩm: {ImportItems.Count}",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                window?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu phiếu nhập: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string GenerateReceiptID()
        {
            try
            {
                var receiptIds = DataProvider.Ins.DB.ImportReceipts
                    .Where(x => x.ReceiptID.StartsWith("PN"))
                    .Select(x => x.ReceiptID)
                    .ToList();

                if (receiptIds.Count == 0)
                    return "PN001";

                int maxId = 0;
                foreach (var id in receiptIds)
                {
                    if (id.Length > 2 && int.TryParse(id.Substring(2), out int num))
                    {
                        if (num > maxId)
                            maxId = num;
                    }
                }

                return "PN" + (maxId + 1).ToString("D3");
            }
            catch
            {
                return "PN001";
            }
        }
    }

    // =================================================================================================
    // IMPORT ITEM CLASS
    // =================================================================================================

    public class ImportItem : BaseVM, IDataErrorInfo
    {
        private string _productID;
        public string ProductID
        {
            get => _productID;
            set { _productID = value; OnPropertyChanged(); }
        }

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

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                RecalculateSubtotal();
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                RecalculateSubtotal();
            }
        }

        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); }
        }

        public void RecalculateSubtotal()
        {
            Subtotal = UnitPrice * Quantity;
        }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(Quantity):
                        if (Quantity <= 0)
                            error = "Số lượng phải lớn hơn 0.";
                        break;
                    case nameof(UnitPrice):
                        if (UnitPrice <= 0)
                            error = "Đơn giá phải lớn hơn 0.";
                        break;
                }
                return error;
            }
        }
    }
}