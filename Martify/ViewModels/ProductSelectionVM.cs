using Martify.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Martify.ViewModels
{
    public class CartItem : BaseVM
    {
        public Product Product { get; set; }
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); }
        }
        public decimal TotalAmount => Product.Price * Quantity;
    }

    public class ProductSelectionVM : BaseVM
    {
        private ObservableCollection<Product> _productList;
        public ObservableCollection<Product> ProductList { get => _productList; set { _productList = value; OnPropertyChanged(); } }

        private ObservableCollection<Product> _allProducts;

        // --- FILTER PROPERTIES ---
        // 1. Danh sách loại sản phẩm
        private ObservableCollection<ProductCategory> _categoryList;
        public ObservableCollection<ProductCategory> CategoryList { get => _categoryList; set { _categoryList = value; OnPropertyChanged(); } }

        private ProductCategory _selectedCategory;
        public ProductCategory SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); FilterProducts(); }
        }

        // 2. Khoảng giá (Dùng string để dễ xử lý rỗng)
        private string _priceFrom;
        public string PriceFrom
        {
            get => _priceFrom;
            set { _priceFrom = value; OnPropertyChanged(); FilterProducts(); }
        }

        private string _priceTo;
        public string PriceTo
        {
            get => _priceTo;
            set { _priceTo = value; OnPropertyChanged(); FilterProducts(); }
        }

        private ObservableCollection<CartItem> _cartList;
        public ObservableCollection<CartItem> CartList { get => _cartList; set { _cartList = value; OnPropertyChanged(); } }

        private decimal _grandTotal;
        public decimal GrandTotal { get => _grandTotal; set { _grandTotal = value; OnPropertyChanged(); } }

        private string _keyword;
        public string Keyword { get => _keyword; set { _keyword = value; OnPropertyChanged(); FilterProducts(); } }

        // --- COMMANDS ---
        public ICommand AddToCartCommand { get; set; }
        public ICommand RemoveFromCartCommand { get; set; }
        public ICommand CheckoutCommand { get; set; }
        public ICommand ClearCartCommand { get; set; }
        public ICommand IncreaseQuantityCommand { get; set; }
        public ICommand DecreaseQuantityCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; } // Nút xóa bộ lọc

        public ProductSelectionVM()
        {
            CartList = new ObservableCollection<CartItem>();
            LoadData(); // Load cả Sản phẩm và Danh mục

            // ... (Các Command Giỏ hàng giữ nguyên như cũ) ...
            AddToCartCommand = new RelayCommand<Product>((p) => p != null, (p) =>
            {
                if (p.StockQuantity <= 0) { MessageBox.Show("Sản phẩm này đã hết hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                var item = CartList.FirstOrDefault(x => x.Product.ProductID == p.ProductID);
                if (item != null)
                {
                    if (item.Quantity + 1 <= p.StockQuantity) { item.Quantity++; CalculateTotal(); }
                }
                else { CartList.Add(new CartItem { Product = p, Quantity = 1 }); CalculateTotal(); }
            });

            RemoveFromCartCommand = new RelayCommand<CartItem>((p) => p != null, (p) => { CartList.Remove(p); CalculateTotal(); });
            ClearCartCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) => { CartList.Clear(); CalculateTotal(); });
            CheckoutCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) => Checkout());
            IncreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity < p.Product.StockQuantity) { p.Quantity++; CalculateTotal(); }
            });
            DecreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity > 1) { p.Quantity--; CalculateTotal(); }
                else if (p.Quantity == 1) { CartList.Remove(p); CalculateTotal(); }
            });

            // MỚI: Xóa bộ lọc
            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedCategory = null;
                PriceFrom = string.Empty;
                PriceTo = string.Empty;
            });
        }

        void LoadData()
        {
            // Load Sản phẩm
            var list = DataProvider.Ins.DB.Products.AsNoTracking().ToList();
            _allProducts = new ObservableCollection<Product>(list);
            ProductList = new ObservableCollection<Product>(list);

            // Load Danh mục
            var cats = DataProvider.Ins.DB.ProductCategories.AsNoTracking().ToList();
            CategoryList = new ObservableCollection<ProductCategory>(cats);
        }

        void FilterProducts()
        {
            // Bắt đầu với toàn bộ danh sách
            IEnumerable<Product> query = _allProducts;

            // 1. Lọc theo Keyword (Tên hoặc Mã)
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                string key = Keyword.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(key) || p.ProductID.ToLower().Contains(key));
            }

            // 2. Lọc theo Danh mục (Loại sản phẩm)
            if (SelectedCategory != null)
            {
                query = query.Where(p => p.CategoryID == SelectedCategory.CategoryID);
            }

            // 3. Lọc theo Giá A -> B
            // Nếu PriceFrom có giá trị -> Lọc >= PriceFrom
            if (decimal.TryParse(PriceFrom, out decimal minPrice))
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            // Nếu PriceTo có giá trị -> Lọc <= PriceTo
            if (decimal.TryParse(PriceTo, out decimal maxPrice))
            {
                query = query.Where(p => p.Price <= maxPrice);
            }

            // Cập nhật lên giao diện
            ProductList = new ObservableCollection<Product>(query.ToList());
        }

        void CalculateTotal() { GrandTotal = CartList.Sum(x => x.TotalAmount); }

        // --- CẬP NHẬT HÀM CHECKOUT ---
        void Checkout()
        {
            if (MessageBox.Show($"Xác nhận thanh toán hóa đơn trị giá {GrandTotal:N0} VND?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. Tạo và Lưu Hóa Đơn vào Database
                    var currentEmpID = DataProvider.Ins.CurrentAccount?.EmployeeID;
                    if (string.IsNullOrEmpty(currentEmpID))
                    {
                        // Fallback nếu không lấy được User hiện tại (tránh crash)
                        currentEmpID = DataProvider.Ins.DB.Employees.FirstOrDefault()?.EmployeeID;
                    }

                    var invoice = new Invoice
                    {
                        InvoiceID = GenerateInvoiceID(),
                        CreatedDate = DateTime.Now,
                        EmployeeID = currentEmpID,
                        TotalAmount = GrandTotal
                    };

                    DataProvider.Ins.DB.Invoices.Add(invoice);

                    foreach (var item in CartList)
                    {
                        var detail = new InvoiceDetail
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProductID = item.Product.ProductID,
                            Quantity = item.Quantity,
                            SalePrice = item.Product.Price
                        };
                        DataProvider.Ins.DB.InvoiceDetails.Add(detail);

                        // Trừ tồn kho
                        var prodInDb = DataProvider.Ins.DB.Products.Find(item.Product.ProductID);
                        if (prodInDb != null) prodInDb.StockQuantity -= item.Quantity;
                    }

                    DataProvider.Ins.DB.SaveChanges();

                    // 2. Lấy lại thông tin đầy đủ của Hóa Đơn vừa tạo (để hiển thị lên View in)
                    // Cần Include để lấy Tên Nhân Viên và Tên Sản Phẩm
                    var fullInvoice = DataProvider.Ins.DB.Invoices
                        .Include(x => x.Employee)
                        .Include(x => x.InvoiceDetails)
                        .ThenInclude(d => d.Product)
                        .FirstOrDefault(x => x.InvoiceID == invoice.InvoiceID);

                    // 3. Hiển thị View In Hóa Đơn (Animation)
                    // (Thay thế cho MessageBox cũ)
                    var printWindow = new Martify.Views.PrinterWindow();
                    printWindow.DataContext = new PrinterVM(fullInvoice);
                    printWindow.ShowDialog();

                    // 4. Dọn dẹp sau khi thanh toán xong
                    CartList.Clear();
                    CalculateTotal();
                    LoadData(); // Load lại để cập nhật số lượng tồn kho mới nhất lên giao diện
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private string GenerateInvoiceID() { return $"HD{DateTime.Now:yyyyMMddHHmmss}"; }
    }
}