using AForge.Video;
using AForge.Video.DirectShow;
using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace Martify.ViewModels
{
    public class CartItem : BaseVM
    {
        public Product Product
        {
            get;
            set;
        }
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

    public class ProductSelectionVM : BaseVM, IDisposable
    {
        private ObservableCollection<Product> _productList;
        public ObservableCollection<Product> ProductList
        {
            get => _productList;
            set
            {
                _productList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Product> _allProducts;

        // --- FILTER PROPERTIES ---
        private ObservableCollection<ProductCategory> _categoryList;
        public ObservableCollection<ProductCategory> CategoryList
        {
            get => _categoryList;
            set
            {
                _categoryList = value;
                OnPropertyChanged();
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

        private string _priceFrom;
        public string PriceFrom
        {
            get => _priceFrom;
            set
            {
                _priceFrom = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        private string _priceTo;
        public string PriceTo
        {
            get => _priceTo;
            set
            {
                _priceTo = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        private ObservableCollection<CartItem> _cartList;
        public ObservableCollection<CartItem> CartList
        {
            get => _cartList;
            set
            {
                _cartList = value;
                OnPropertyChanged();
            }
        }

        private decimal _grandTotal;
        public decimal GrandTotal
        {
            get => _grandTotal;
            set
            {
                _grandTotal = value;
                OnPropertyChanged();
            }
        }

        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                OnPropertyChanged();
                FilterProducts();
            }
        }

        // --- CAMERA & QR PROPERTIES ---
        private VideoCaptureDevice _videoSource;
        private readonly BarcodeReader _barcodeReader;
        private readonly object _frameLock = new object();
        private DateTime _lastScanTime = DateTime.MinValue;
        private const int SCAN_COOLDOWN_MS = 2000;

        private bool _isCameraOpen;
        public bool IsCameraOpen
        {
            get => _isCameraOpen;
            set
            {
                _isCameraOpen = value;
                OnPropertyChanged();
                if (!_isCameraOpen) StopCamera();
            }
        }

        private BitmapImage _cameraFrame;
        public BitmapImage CameraFrame
        {
            get => _cameraFrame;
            set
            {
                _cameraFrame = value;
                OnPropertyChanged();
            }
        }

        private string _scanStatus;
        public string ScanStatus
        {
            get => _scanStatus;
            set
            {
                _scanStatus = value;
                OnPropertyChanged();
            }
        }

        // --- CAMERA SELECTION PROPERTIES ---
        private ObservableCollection<CameraDevice> _cameras;
        public ObservableCollection<CameraDevice> Cameras
        {
            get => _cameras;
            set
            {
                _cameras = value;
                OnPropertyChanged();
            }
        }

        private CameraDevice _selectedCamera;
        public CameraDevice SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    OnPropertyChanged();

                    if (IsCameraOpen && _selectedCamera != null)
                    {
                        StartCamera();
                    }
                }
            }
        }

        // --- COMMANDS ---
        public ICommand AddToCartCommand
        {
            get;
            set;
        }
        public ICommand RemoveFromCartCommand
        {
            get;
            set;
        }
        public ICommand CheckoutCommand
        {
            get;
            set;
        }
        public ICommand ClearCartCommand
        {
            get;
            set;
        }
        public ICommand IncreaseQuantityCommand
        {
            get;
            set;
        }
        public ICommand DecreaseQuantityCommand
        {
            get;
            set;
        }
        public ICommand ClearFilterCommand
        {
            get;
            set;
        }
        public ICommand ToggleCameraCommand
        {
            get;
            set;
        }
        public ICommand RefreshCamerasCommand
        {
            get;
            set;
        }

        public ProductSelectionVM()
        {
            CartList = new ObservableCollection<CartItem>();
            LoadData();

            Cameras = new ObservableCollection<CameraDevice>();
            LoadCameras();

            // [FIX QUAN TRỌNG]: Đăng ký sự kiện tắt App để giết Camera
            if (Application.Current != null)
            {
                Application.Current.Exit += (s, e) => Dispose();
            }

            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE, BarcodeFormat.EAN_13, BarcodeFormat.CODE_128 }
                }
            };

            AddToCartCommand = new RelayCommand<Product>((p) => p != null, (p) => AddToCart(p));
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

            IncreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity < p.Product.StockQuantity)
                {
                    p.Quantity++;
                    CalculateTotal();
                }
            });

            DecreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity > 1)
                {
                    p.Quantity--;
                    CalculateTotal();
                }
                else if (p.Quantity == 1)
                {
                    CartList.Remove(p);
                    CalculateTotal();
                }
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedCategory = null;
                PriceFrom = string.Empty;
                PriceTo = string.Empty;
            });

            ToggleCameraCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                if (IsCameraOpen)
                {
                    IsCameraOpen = false;
                }
                else
                {
                    IsCameraOpen = true;
                    StartCamera();
                }
            });

            RefreshCamerasCommand = new RelayCommand<object>((p) => true, (p) => LoadCameras());
        }

        private void LoadCameras()
        {
            try
            {
                Cameras.Clear();
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                foreach (FilterInfo device in videoDevices)
                {
                    Cameras.Add(new CameraDevice
                    {
                        Name = device.Name,
                        MonikerString = device.MonikerString
                    });
                }

                if (Cameras.Count > 0 && SelectedCamera == null)
                {
                    SelectedCamera = Cameras[0];
                }
                else if (Cameras.Count == 0)
                {
                    SelectedCamera = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách camera: {ex.Message}");
            }
        }

        private bool AddToCart(Product p)
        {
            if (p.StockQuantity <= 0)
            {
                //if (!IsCameraOpen) MessageBox.Show("Sản phẩm này đã hết hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var item = CartList.FirstOrDefault(x => x.Product.ProductID == p.ProductID);
            if (item != null)
            {
                if (item.Quantity + 1 <= p.StockQuantity)
                {
                    item.Quantity++;
                    CalculateTotal();
                    return true;
                }
                else
                {
                    //if (!IsCameraOpen) MessageBox.Show("Số lượng tồn kho không đủ!", "Thông báo");
                    return false;
                }
            }
            else
            {
                CartList.Add(new CartItem { Product = p, Quantity = 1 });
                CalculateTotal();
                return true;
            }
        }

        // --- LOGIC CAMERA & QR ---
        private void StartCamera()
        {
            try
            {
                if (SelectedCamera == null)
                {
                    if (Cameras.Count == 0) LoadCameras();
                    if (SelectedCamera == null)
                    {
                        MessageBox.Show("Không tìm thấy camera nào!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsCameraOpen = false;
                        return;
                    }
                }

                StopCamera();

                _videoSource = new VideoCaptureDevice(SelectedCamera.MonikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
                ScanStatus = "Đang quét...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động camera: {ex.Message}");
                IsCameraOpen = false;
            }
        }

        public void StopCamera()
        {
            if (_videoSource != null)
            {
                // [FIX QUAN TRỌNG]: Ngắt sự kiện trước khi dừng
                _videoSource.NewFrame -= VideoSource_NewFrame;

                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    // Không dùng WaitForStop() để tránh treo UI, để nó tự tắt ngầm
                }
                _videoSource = null;
            }

            CameraFrame = null;
            if (IsCameraOpen) ScanStatus = "Camera đã tắt"; // Chỉ hiện nếu đang bật mà bị tắt
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (!_isCameraOpen || Application.Current == null) return;

                Bitmap bitmap;
                lock (_frameLock)
                {
                    bitmap = (Bitmap)eventArgs.Frame.Clone();
                }

                var bi = BitmapToBitmapImage(bitmap);
                bi.Freeze();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CameraFrame = bi;
                });

                if ((DateTime.Now - _lastScanTime).TotalMilliseconds >= SCAN_COOLDOWN_MS)
                {
                    var result = _barcodeReader.Decode(bitmap);
                    if (result != null)
                    {
                        _lastScanTime = DateTime.Now;
                        string scannedCode = result.Text;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProcessScannedProduct(scannedCode);
                        });
                    }
                }

                bitmap.Dispose();
            }
            catch
            {
            }
        }

        private void ProcessScannedProduct(string code)
        {
            var product = _allProducts.FirstOrDefault(p => p.ProductID == code);

            if (product != null)
            {
                bool isAdded = AddToCart(product);
                if (isAdded)
                {
                    ScanStatus = $"Đã thêm: {product.ProductName}";
                }
                else
                {
                    ScanStatus = $"HẾT HÀNG: {product.ProductName}";
                }
            }
            else
            {
                ScanStatus = $"Không tìm thấy SP: {code}";
            }

            ResetScanStatus();
        }

        private async void ResetScanStatus()
        {
            try
            {
                await Task.Delay(2000);
                if (IsCameraOpen)
                {
                    ScanStatus = "Đang quét...";
                }
            }
            catch
            {
            }
        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public void Dispose()
        {
            StopCamera();
        }

        // --- CÁC HÀM CŨ ---
        void LoadData()
        {
            var list = DataProvider.Ins.DB.Products.AsNoTracking().ToList();
            _allProducts = new ObservableCollection<Product>(list);
            ProductList = new ObservableCollection<Product>(list);

            var cats = DataProvider.Ins.DB.ProductCategories.AsNoTracking().ToList();
            CategoryList = new ObservableCollection<ProductCategory>(cats);
        }

        void FilterProducts()
        {
            IEnumerable<Product> query = _allProducts;

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                string key = Keyword.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(key) || p.ProductID.ToLower().Contains(key));
            }

            if (SelectedCategory != null)
                query = query.Where(p => p.CategoryID == SelectedCategory.CategoryID);

            if (decimal.TryParse(PriceFrom, out decimal minPrice))
                query = query.Where(p => p.Price >= minPrice);

            if (decimal.TryParse(PriceTo, out decimal maxPrice))
                query = query.Where(p => p.Price <= maxPrice);

            ProductList = new ObservableCollection<Product>(query.ToList());
        }

        void CalculateTotal()
        {
            GrandTotal = CartList.Sum(x => x.TotalAmount);
        }

        void Checkout()
        {
            if (MessageBox.Show($"Xác nhận thanh toán hóa đơn trị giá {GrandTotal:N0} VND?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var currentEmpID = DataProvider.Ins.CurrentAccount?.EmployeeID;
                    if (string.IsNullOrEmpty(currentEmpID))
                        currentEmpID = DataProvider.Ins.DB.Employees.FirstOrDefault()?.EmployeeID;

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

                        var prodInDb = DataProvider.Ins.DB.Products.Find(item.Product.ProductID);
                        if (prodInDb != null) prodInDb.StockQuantity -= item.Quantity;
                    }

                    DataProvider.Ins.DB.SaveChanges();

                    var fullInvoice = DataProvider.Ins.DB.Invoices
                        .Include(x => x.Employee)
                        .Include(x => x.InvoiceDetails)
                        .ThenInclude(d => d.Product)
                        .FirstOrDefault(x => x.InvoiceID == invoice.InvoiceID);

                    var printWindow = new Martify.Views.PrinterWindow();
                    printWindow.DataContext = new PrinterVM(fullInvoice);
                    printWindow.ShowDialog();

                    CartList.Clear();
                    CalculateTotal();
                    LoadData();
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