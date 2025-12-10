using Martify.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Martify.ViewModels
{
    public class CartItem : BaseVM
    {
        public Product Product { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public decimal TotalAmount => Product.Price * Quantity;
    }

    public class ProductSelectionVM : BaseVM
    {
        // ... (Các khai báo cũ giữ nguyên)
        private ObservableCollection<Product> _productList;
        public ObservableCollection<Product> ProductList { get => _productList; set { _productList = value; OnPropertyChanged(); } }

        private ObservableCollection<Product> _allProducts;

        private ObservableCollection<CartItem> _cartList;
        public ObservableCollection<CartItem> CartList { get => _cartList; set { _cartList = value; OnPropertyChanged(); } }

        private decimal _grandTotal;
        public decimal GrandTotal { get => _grandTotal; set { _grandTotal = value; OnPropertyChanged(); } }

        private string _keyword;
        public string Keyword { get => _keyword; set { _keyword = value; OnPropertyChanged(); FilterProducts(); } }

        public ICommand AddToCartCommand { get; set; }
        public ICommand RemoveFromCartCommand { get; set; }
        public ICommand CheckoutCommand { get; set; }
        public ICommand ClearCartCommand { get; set; }

        // MỚI: Command tăng giảm số lượng
        public ICommand IncreaseQuantityCommand { get; set; }
        public ICommand DecreaseQuantityCommand { get; set; }

        public ProductSelectionVM()
        {
            CartList = new ObservableCollection<CartItem>();
            LoadProducts();

            // LOGIC THÊM VÀO GIỎ (CÓ CHECK TỒN KHO)
            AddToCartCommand = new RelayCommand<Product>((p) => p != null, (p) =>
            {
                if (p.StockQuantity <= 0)
                {
                    MessageBox.Show("Sản phẩm này đã hết hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var item = CartList.FirstOrDefault(x => x.Product.ProductID == p.ProductID);
                if (item != null)
                {
                    // Đã có trong giỏ -> Kiểm tra nếu tăng thêm 1 có vượt tồn kho không
                    if (item.Quantity + 1 <= p.StockQuantity)
                    {
                        item.Quantity++;
                        CalculateTotal();
                    }
                    //else
                    //{
                    //    MessageBox.Show($"Chỉ còn {p.StockQuantity} sản phẩm trong kho!", "Hết hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //}
                }
                else
                {
                    // Chưa có -> Thêm mới
                    CartList.Add(new CartItem { Product = p, Quantity = 1 });
                    CalculateTotal();
                }
            });

            RemoveFromCartCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                CartList.Remove(p);
                CalculateTotal();
            });

            ClearCartCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) =>
            {
                CartList.Clear();
                CalculateTotal();
            });

            CheckoutCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) => Checkout());

            // LOGIC TĂNG SỐ LƯỢNG (+)
            IncreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity < p.Product.StockQuantity)
                {
                    p.Quantity++;
                    CalculateTotal();
                }
                //else
                //{
                //    MessageBox.Show($"Đã đạt giới hạn tồn kho ({p.Product.StockQuantity})!", "Thông báo");
                //}
            });

            // LOGIC GIẢM SỐ LƯỢNG (-)
            DecreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity > 1)
                {
                    p.Quantity--;
                    CalculateTotal();
                }
                // Nếu muốn giảm về 0 là xóa thì thêm logic ở đây, hiện tại giữ tối thiểu là 1
            });
        }

        void LoadProducts()
        {
            var list = DataProvider.Ins.DB.Products.AsNoTracking().ToList();
            _allProducts = new ObservableCollection<Product>(list);
            ProductList = new ObservableCollection<Product>(list);
        }

        void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                ProductList = new ObservableCollection<Product>(_allProducts);
            }
            else
            {
                string key = Keyword.ToLower();
                var filtered = _allProducts.Where(p =>
                    p.ProductName.ToLower().Contains(key) ||
                    p.ProductID.ToLower().Contains(key)).ToList();
                ProductList = new ObservableCollection<Product>(filtered);
            }
        }

        void CalculateTotal()
        {
            GrandTotal = CartList.Sum(x => x.TotalAmount);
        }

        void Checkout()
        {
            if (MessageBox.Show($"Xác nhận thanh toán hóa đơn trị giá {GrandTotal:N0} VND?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var invoice = new Invoice
                    {
                        InvoiceID = GenerateInvoiceID(),
                        CreatedDate = DateTime.Now,
                        EmployeeID = DataProvider.Ins.CurrentAccount.EmployeeID,
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

                        var prodInDb = DataProvider.Ins.DB.Products.Find(item.Product.ProductID);
                        if (prodInDb != null)
                        {
                            // Trừ tồn kho (đã được validate ở UI nên không sợ âm, nhưng code an toàn vẫn trừ)
                            prodInDb.StockQuantity -= item.Quantity;
                        }
                    }

                    DataProvider.Ins.DB.SaveChanges();
                    MessageBox.Show($"Xuất hóa đơn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    CartList.Clear();
                    CalculateTotal();
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateInvoiceID()
        {
            return $"HD{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}