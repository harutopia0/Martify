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

        private List<Product> _allProducts;
        public List<Product> AllProducts
        {
            get => _allProducts;
            set { _allProducts = value; OnPropertyChanged(); }
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
            set
            {
                _selectedSupplierID = value;
                OnPropertyChanged();
                FilterProductsBySupplier();
            }
        }

        private string _selectedSupplierName;
        private string _lastCheckedSupplierName; // Biến để track tên đã check

        public string SelectedSupplierName
        {
            get => _selectedSupplierName;
            set
            {
                _selectedSupplierName = value;
                OnPropertyChanged();

                // Chỉ xử lý khi text thay đổi và không rỗng
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Kiểm tra xem có tồn tại trong danh sách không
                    var existingSupplier = SupplierList?.FirstOrDefault(s =>
                        s.SupplierName.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existingSupplier != null)
                    {
                        // Nếu tồn tại, set ID
                        SelectedSupplierID = existingSupplier.SupplierID;
                        _lastCheckedSupplierName = value.Trim();
                    }
                }
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
        public ICommand AddSupplierCommand { get; set; }
        public ICommand ValidateSupplierCommand { get; set; } // Command mới cho validation

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

        public ImportProductsVM(IEnumerable<Product> products, int defaultQuantity = 50)
        {
            ImportItems = new ObservableCollection<ImportItem>();
            LoadProducts();
            LoadSuppliers();
            InitializeCommands();

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

            // Command mới để validate khi blur hoặc enter
            ValidateSupplierCommand = new RelayCommand<object>(
                (p) => true,
                (p) => ValidateSupplierName());

            ImportItems.CollectionChanged += (s, e) => CalculateTotal();
        }

        // =================================================================================================
        // METHODS
        // =================================================================================================

        /// <summary>
        /// Method mới: Validate tên nhà cung cấp khi user hoàn tất nhập
        /// </summary>
        public void ValidateSupplierName()
        {
            if (string.IsNullOrWhiteSpace(SelectedSupplierName))
                return;

            var trimmedName = SelectedSupplierName.Trim();

            // Kiểm tra xem đã validate tên này chưa
            if (trimmedName == _lastCheckedSupplierName)
                return;

            // Kiểm tra xem nhà cung cấp có tồn tại không
            var existingSupplier = SupplierList?.FirstOrDefault(s =>
                s.SupplierName.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));

            if (existingSupplier != null)
            {
                // Nhà cung cấp đã tồn tại
                SelectedSupplierID = existingSupplier.SupplierID;
                _lastCheckedSupplierName = trimmedName;
            }
            else
            {
                // Nhà cung cấp chưa tồn tại -> hỏi user
                _lastCheckedSupplierName = trimmedName;
                HandleNewSupplier(trimmedName);
            }
        }

        private void LoadProducts()
        {
            try
            {
                AllProducts = DataProvider.Ins.DB.Products
                    .OrderBy(p => p.ProductName)
                    .ToList();

                FilteredProducts = new ObservableCollection<Product>(AllProducts);
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
                SupplierList = DataProvider.Ins.DB.Suppliers.OrderBy(s => s.SupplierName).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterProductsBySupplier()
        {
            if (string.IsNullOrWhiteSpace(SelectedSupplierID))
            {
                FilterProducts();
                return;
            }

            try
            {
                var supplierProductIds = DataProvider.Ins.DB.ImportReceiptDetails
                    .Where(ird => ird.ImportReceipt.SupplierID == SelectedSupplierID)
                    .Select(ird => ird.ProductID)
                    .Distinct()
                    .ToList();

                var filteredList = AllProducts
                    .Where(p => supplierProductIds.Contains(p.ProductID))
                    .ToList();

                if (!filteredList.Any())
                {
                    filteredList = AllProducts.ToList();
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.Trim().ToLower();
                    filteredList = filteredList
                        .Where(p =>
                            p.ProductID.ToLower().Contains(search) ||
                            p.ProductName.ToLower().Contains(search))
                        .ToList();
                }

                FilteredProducts = new ObservableCollection<Product>(filteredList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lọc sản phẩm: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterProducts()
        {
            if (!string.IsNullOrWhiteSpace(SelectedSupplierID))
            {
                FilterProductsBySupplier();
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredProducts = new ObservableCollection<Product>(AllProducts);
                return;
            }

            var search = SearchText.Trim().ToLower();
            var filtered = AllProducts
                .Where(p =>
                    p.ProductID.ToLower().Contains(search) ||
                    p.ProductName.ToLower().Contains(search))
                .ToList();

            FilteredProducts = new ObservableCollection<Product>(filtered);
        }

        private void AddProductToImport(Product product)
        {
            if (product == null) return;

            if (string.IsNullOrWhiteSpace(SelectedSupplierID))
            {
                AutoSelectSupplierForProduct(product.ProductID);
            }

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

        private void AutoSelectSupplierForProduct(string productID)
        {
            try
            {
                var lastSupplier = DataProvider.Ins.DB.ImportReceiptDetails
                    .Where(ird => ird.ProductID == productID)
                    .OrderByDescending(ird => ird.ImportReceipt.ImportDate)
                    .Select(ird => ird.ImportReceipt.SupplierID)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(lastSupplier))
                {
                    SelectedSupplierID = lastSupplier;

                    var supplier = SupplierList.FirstOrDefault(s => s.SupplierID == lastSupplier);
                    if (supplier != null)
                    {
                        _selectedSupplierName = supplier.SupplierName;
                        _lastCheckedSupplierName = supplier.SupplierName;
                        OnPropertyChanged(nameof(SelectedSupplierName));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể tự động chọn nhà cung cấp: {ex.Message}");
            }
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
                string receiptID = GenerateReceiptID();
                var user = DataProvider.Ins.CurrentAccount;

                var receipt = new ImportReceipt
                {
                    ReceiptID = receiptID,
                    EmployeeID = user.EmployeeID,
                    SupplierID = SelectedSupplierID,
                    ImportDate = DateTime.Now,
                    TotalAmount = TotalAmount
                };

                DataProvider.Ins.DB.ImportReceipts.Add(receipt);

                foreach (var item in ImportItems)
                {
                    var product = DataProvider.Ins.DB.Products
                        .FirstOrDefault(p => p.ProductID == item.ProductID);

                    if (product != null)
                    {
                        if (product.Price != item.UnitPrice)
                        {
                            var newProduct = new Product
                            {
                                ProductID = GenerateProductID(),
                                ProductName = product.ProductName,
                                Unit = product.Unit,
                                Price = item.UnitPrice,
                                StockQuantity = 0,
                                CategoryID = product.CategoryID,
                                ImagePath = product.ImagePath
                            };

                            item.ProductID = newProduct.ProductID;
                            DataProvider.Ins.DB.Products.Add(newProduct);
                            newProduct.StockQuantity += item.Quantity;
                        }
                        else
                            product.StockQuantity += item.Quantity;

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

        private void HandleNewSupplier(string newName)
        {
            var result = MessageBox.Show(
                $"Nhà cung cấp '{newName}' chưa có trong hệ thống. Bạn có muốn thêm mới không?",
                "Thông báo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AddNewSupplier(newName);
            }
            else
            {
                // Reset về rỗng nếu user không muốn thêm
                _selectedSupplierName = string.Empty;
                _lastCheckedSupplierName = string.Empty;
                OnPropertyChanged(nameof(SelectedSupplierName));
            }
        }

        private void AddNewSupplier(string suggestedName)
        {
            try
            {
                var addSupplierWindow = new Martify.Views.AddSupplier();

                // Truyền tên gợi ý vào ViewModel của AddSupplier nếu cần
                if (addSupplierWindow.DataContext is AddSupplierVM addSupplierVM)
                {
                    addSupplierVM.SupplierName = suggestedName;
                }

                if (addSupplierWindow.ShowDialog() == true)
                {
                    LoadSuppliers();

                    var newSupplier = SupplierList.FirstOrDefault(s =>
                        s.SupplierName.Equals(suggestedName, StringComparison.OrdinalIgnoreCase))
                                     ?? SupplierList.LastOrDefault();

                    if (newSupplier != null)
                    {
                        SelectedSupplierID = newSupplier.SupplierID;
                        _selectedSupplierName = newSupplier.SupplierName;
                        _lastCheckedSupplierName = newSupplier.SupplierName;
                        OnPropertyChanged(nameof(SelectedSupplierName));
                    }
                }
                else
                {
                    // Nếu user hủy dialog thêm supplier
                    _selectedSupplierName = string.Empty;
                    _lastCheckedSupplierName = string.Empty;
                    OnPropertyChanged(nameof(SelectedSupplierName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ thêm nhà cung cấp: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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