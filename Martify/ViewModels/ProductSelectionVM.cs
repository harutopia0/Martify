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
using System.Media;
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
    // Class lưu trữ đơn hàng tạm giữ
    public class HeldOrder
    {
        public DateTime SavedTime
        {
            get;
            set;
        } = DateTime.Now;
        public List<CartItem> Items
        {
            get;
            set;
        }
        public decimal TotalAmount
        {
            get;
            set;
        }
        public string DisplayInfo => $"{SavedTime:HH:mm} - {Items.Count} SP";
        public string DisplayTotal => $"{TotalAmount:N0}";
    }

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
        // [MỚI] Hàm Static để xóa dữ liệu khi Logout (Gọi từ SettingsVM)
        public static void ClearStaticData()
        {
            _staticHeldOrders.Clear();
        }

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

        // --- [MỚI] PROPERTIES CHO TẠM GIỮ ĐƠN HÀNG ---
        // [QUAN TRỌNG]: Static để giữ dữ liệu khi chuyển Tab
        private static ObservableCollection<HeldOrder> _staticHeldOrders = new ObservableCollection<HeldOrder>();

        public ObservableCollection<HeldOrder> HeldOrders
        {
            get => _staticHeldOrders;
            set
            {
                _staticHeldOrders = value;
                OnPropertyChanged();
            }
        }

        private bool _isHeldListVisible;
        public bool IsHeldListVisible
        {
            get => _isHeldListVisible;
            set
            {
                _isHeldListVisible = value;
                OnPropertyChanged();
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

        // [MỚI] Commands Tạm giữ
        public ICommand ParkOrderCommand
        {
            get;
            set;
        }
        public ICommand ToggleHeldListCommand
        {
            get;
            set;
        }
        public ICommand RestoreHeldOrderCommand
        {
            get;
            set;
        }
        public ICommand DeleteHeldOrderCommand
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

            // Đăng ký sự kiện tắt App để giết Camera
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
                CommandManager.InvalidateRequerySuggested();
            });
            ClearCartCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) =>
            {
                CartList.Clear();
                CalculateTotal();
                CommandManager.InvalidateRequerySuggested();
            });
            CheckoutCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) => Checkout());

            IncreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                int reservedInHeld = GetReservedQuantity(p.Product.ProductID);
                int actualAvailable = p.Product.StockQuantity - reservedInHeld;

                if (p.Quantity + 1 <= actualAvailable)
                {
                    p.Quantity++;
                    CalculateTotal();
                    CommandManager.InvalidateRequerySuggested();
                }
            });

            DecreaseQuantityCommand = new RelayCommand<CartItem>((p) => p != null, (p) =>
            {
                if (p.Quantity > 1) p.Quantity--;
                else if (p.Quantity == 1) CartList.Remove(p);
                CalculateTotal();
                CommandManager.InvalidateRequerySuggested();
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
                if (IsCameraOpen) IsCameraOpen = false;
                else
                {
                    IsCameraOpen = true;
                    StartCamera();
                }
            });
            RefreshCamerasCommand = new RelayCommand<object>((p) => true, (p) => LoadCameras());

            // --- COMMANDS TẠM GIỮ ---
            ParkOrderCommand = new RelayCommand<object>((p) => CartList.Count > 0, (p) => ParkOrder());

            // [FIX] Chỉ cho phép bật danh sách nếu có đơn hàng đang giữ
            ToggleHeldListCommand = new RelayCommand<object>(
                (p) => HeldOrders != null && HeldOrders.Count > 0,
                (p) => IsHeldListVisible = !IsHeldListVisible
            );

            RestoreHeldOrderCommand = new RelayCommand<HeldOrder>((p) => p != null, RestoreHeldOrder);

            // Xóa đơn treo -> Cập nhật trạng thái nút
            DeleteHeldOrderCommand = new RelayCommand<HeldOrder>((p) => p != null, (p) =>
            {
                HeldOrders.Remove(p);
                if (HeldOrders.Count == 0) IsHeldListVisible = false;

                // [QUAN TRỌNG]: Bắt buộc gọi lệnh này để nút ToggleHeldListCommand tự disable đi
                CommandManager.InvalidateRequerySuggested();
            });
        }

        // [MỚI] Helper tính số lượng đang bị treo
        private int GetReservedQuantity(string productId)
        {
            if (HeldOrders == null) return 0;
            return HeldOrders.Sum(order => order.Items
                .Where(item => item.Product.ProductID == productId)
                .Sum(item => item.Quantity));
        }

        // [MỚI] Logic Park Order
        private void ParkOrder()
        {
            var snapshot = new List<CartItem>(CartList);
            HeldOrders.Add(new HeldOrder
            {
                Items = snapshot,
                TotalAmount = GrandTotal
            });
            CartList.Clear();
            CalculateTotal();

            // [QUAN TRỌNG]: Cập nhật UI để nút "Đơn treo" sáng lên
            CommandManager.InvalidateRequerySuggested();
        }

        private void RestoreHeldOrder(HeldOrder order)
        {
            CartList = new ObservableCollection<CartItem>(order.Items);
            CalculateTotal();
            HeldOrders.Remove(order);
            IsHeldListVisible = false;

            // [QUAN TRỌNG]: Cập nhật UI để nút "Đơn treo" mờ đi nếu hết đơn
            CommandManager.InvalidateRequerySuggested();
        }
        // --- End Logic Park Order ---

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
            // Tính toán số lượng thực tế
            int reservedInHeld = GetReservedQuantity(p.ProductID);
            int actualAvailable = p.StockQuantity - reservedInHeld;

            if (actualAvailable <= 0)
            {
                return false;
            }

            var item = CartList.FirstOrDefault(x => x.Product.ProductID == p.ProductID);
            if (item != null)
            {
                if (item.Quantity + 1 <= actualAvailable)
                {
                    item.Quantity++;
                    CalculateTotal();
                    CommandManager.InvalidateRequerySuggested();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (actualAvailable >= 1)
                {
                    CartList.Add(new CartItem { Product = p, Quantity = 1 });
                    CalculateTotal();
                    CommandManager.InvalidateRequerySuggested();
                    return true;
                }
                return false;
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
                _videoSource.NewFrame -= VideoSource_NewFrame;

                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                }
                _videoSource = null;
            }

            CameraFrame = null;
            if (IsCameraOpen) ScanStatus = "Camera đã tắt";
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
                    PlaySound("store-scanner-beep.wav");
                }
                else
                {
                    ScanStatus = $"HẾT HÀNG: {product.ProductName}";
                    SystemSounds.Exclamation.Play();
                }
            }
            else
            {
                ScanStatus = $"Không tìm thấy SP: {code}";
                SystemSounds.Hand.Play();
            }

            ResetScanStatus();
        }

        private void PlaySound(string fileName)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds", fileName);
                if (File.Exists(path))
                {
                    using (var player = new SoundPlayer(path))
                    {
                        player.Play();
                    }
                }
            }
            catch
            {
            }
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
                        if (prodInDb != null)
                        {
                            if (prodInDb.StockQuantity < item.Quantity)
                            {
                                throw new Exception($"Sản phẩm {prodInDb.ProductName} không đủ tồn kho (Còn {prodInDb.StockQuantity}).");
                            }
                            prodInDb.StockQuantity -= item.Quantity;
                        }
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
                    LoadData();
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