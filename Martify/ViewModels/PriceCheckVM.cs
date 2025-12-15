using AForge.Video;
using AForge.Video;
using AForge.Video.DirectShow;
using Martify.Models;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace Martify.ViewModels
{
    public class PriceCheckVM : BaseVM, IDisposable
    {
        #region Private Fields
        private readonly MartifyDbContext _dbContext;
        private FilterInfoCollection? _videoDevices;
        private VideoCaptureDevice? _videoSource;
        private readonly BarcodeReader _barcodeReader;
        private bool _isScanning;
        private bool _disposed;
        private readonly object _frameLock = new object();
        private DateTime _lastScanTime = DateTime.MinValue;
        private const int SCAN_COOLDOWN_MS = 1500;
        #endregion

        #region Observable Properties
        private ObservableCollection<CameraDevice> _cameras = new();
        public ObservableCollection<CameraDevice> Cameras
        {
            get => _cameras;
            set { _cameras = value; OnPropertyChanged(); }
        }

        private CameraDevice? _selectedCamera;
        public CameraDevice? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    OnPropertyChanged();
                    OnCameraChanged();
                }
            }
        }

        private BitmapImage? _cameraFrame;
        public BitmapImage? CameraFrame
        {
            get => _cameraFrame;
            set { _cameraFrame = value; OnPropertyChanged(); }
        }

        private bool _isCameraActive;
        public bool IsCameraActive
        {
            get => _isCameraActive;
            set { _isCameraActive = value; OnPropertyChanged(); }
        }

        private bool _isProductFound;
        public bool IsProductFound
        {
            get => _isProductFound;
            set { _isProductFound = value; OnPropertyChanged(); }
        }

        private bool _isProductNotFound;
        public bool IsProductNotFound
        {
            get => _isProductNotFound;
            set { _isProductNotFound = value; OnPropertyChanged(); }
        }

        private string? _productName;
        public string? ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        private string? _productId;
        public string? ProductId
        {
            get => _productId;
            set { _productId = value; OnPropertyChanged(); }
        }

        private decimal _productPrice;
        public decimal ProductPrice
        {
            get => _productPrice;
            set { _productPrice = value; OnPropertyChanged(); }
        }

        private string? _formattedPrice;
        public string? FormattedPrice
        {
            get => _formattedPrice;
            set { _formattedPrice = value; OnPropertyChanged(); }
        }

        private int _stockQuantity;
        public int StockQuantity
        {
            get => _stockQuantity;
            set { _stockQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(StockStatus)); OnPropertyChanged(nameof(StockStatusDisplay)); OnPropertyChanged(nameof(StockStatusColor)); }
        }

        private string? _productUnit;
        public string? ProductUnit
        {
            get => _productUnit;
            set { _productUnit = value; OnPropertyChanged(); }
        }

        private string? _categoryName;
        public string? CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(); }
        }

        private string? _lastScannedCode;
        public string? LastScannedCode
        {
            get => _lastScannedCode;
            set { _lastScannedCode = value; OnPropertyChanged(); }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string StockStatus
        {
            get
            {
                if (StockQuantity == 0) return "OutOfStock";
                if (StockQuantity <= 10) return "LowStock";
                return "InStock";
            }
        }

        public string StockStatusDisplay
        {
            get
            {
                if (StockQuantity == 0) return "Out of Stock";
                if (StockQuantity <= 10) return "Low Stock";
                return "In Stock";
            }
        }

        public string StockStatusColor
        {
            get
            {
                if (StockQuantity == 0) return "#F44336";
                if (StockQuantity <= 10) return "#FF9800";
                return "#4CAF50";
            }
        }
        #endregion

        #region Commands
        public ICommand CloseCommand { get; }
        public ICommand ResetScanCommand { get; }
        public ICommand RefreshCamerasCommand { get; }
        #endregion

        #region Constructor
        public PriceCheckVM()
        {
            _dbContext = new MartifyDbContext();

            // Initialize ZXing barcode reader with optimized settings
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39 }
                }
            };

            // Initialize commands
            CloseCommand = new RelayCommand<Window>(
                canExecute: _ => true,
                execute: window => window?.Close()
            );

            ResetScanCommand = new RelayCommand<object>(
                canExecute: _ => true,
                execute: _ => ResetScan()
            );

            RefreshCamerasCommand = new RelayCommand<object>(
                canExecute: _ => true,
                execute: _ => LoadCameras()
            );

            StatusMessage = "Ðang kh?i t?o camera...";
            LoadCameras();
        }
        #endregion

        #region Camera Management
        private void LoadCameras()
        {
            try
            {
                Cameras.Clear();
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    StatusMessage = "Không t?m th?y camera nào!";
                    return;
                }

                foreach (FilterInfo device in _videoDevices)
                {
                    Cameras.Add(new CameraDevice
                    {
                        Name = device.Name,
                        MonikerString = device.MonikerString
                    });
                }

                // Auto-select first camera
                if (Cameras.Count > 0)
                {
                    SelectedCamera = Cameras[0];
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"L?i khi t?i danh sách camera: {ex.Message}";
            }
        }

        private void OnCameraChanged()
        {
            StopCamera();

            if (SelectedCamera != null)
            {
                StartCamera(SelectedCamera.MonikerString);
            }
        }

        public void StartCamera(string monikerString)
        {
            try
            {
                StopCamera();

                _videoSource = new VideoCaptureDevice(monikerString);
                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();

                _isScanning = true;
                IsCameraActive = true;
                StatusMessage = "Ðýa m? QR/Barcode vào khung h?nh ð? quét...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"L?i khi kh?i ð?ng camera: {ex.Message}";
                IsCameraActive = false;
            }
        }

        public void StopCamera()
        {
            _isScanning = false;

            if (_videoSource != null)
            {
                try
                {
                    _videoSource.NewFrame -= VideoSource_NewFrame;

                    if (_videoSource.IsRunning)
                    {
                        _videoSource.SignalToStop();
                        _videoSource.WaitForStop();
                    }
                }
                catch { }
                finally
                {
                    _videoSource = null;
                }
            }

            IsCameraActive = false;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!_isScanning || _disposed) return;

            Bitmap? bitmap = null;
            try
            {
                lock (_frameLock)
                {
                    // Clone the bitmap to avoid cross-thread issues
                    bitmap = (Bitmap)eventArgs.Frame.Clone();
                }

                // Update UI with camera frame
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        CameraFrame = BitmapToBitmapImage(bitmap);
                    }
                    catch { }
                });

                // Only scan if cooldown has passed
                if ((DateTime.Now - _lastScanTime).TotalMilliseconds >= SCAN_COOLDOWN_MS)
                {
                    // Try to decode barcode/QR
                    var result = _barcodeReader.Decode(bitmap);
                    if (result != null)
                    {
                        _lastScanTime = DateTime.Now;
                        ProcessScannedCode(result.Text);
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore frame processing errors
            }
            finally
            {
                // CRITICAL: Dispose bitmap to prevent memory leak
                bitmap?.Dispose();
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
                bitmapImage.Freeze(); // Important for cross-thread access

                return bitmapImage;
            }
        }
        #endregion

        #region Product Lookup
        private void ProcessScannedCode(string code)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LastScannedCode = code;
                StatusMessage = $"Ð? quét: {code}";

                // Freeze scanning while showing result
                _isScanning = false;

                // Look up product in database
                var product = _dbContext.Products
                    .Where(p => p.ProductID == code)
                    .Select(p => new
                    {
                        p.ProductID,
                        p.ProductName,
                        p.Price,
                        p.StockQuantity,
                        p.Unit,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "N/A"
                    })
                    .FirstOrDefault();

                if (product != null)
                {
                    ProductId = product.ProductID;
                    ProductName = product.ProductName;
                    ProductPrice = product.Price;
                    FormattedPrice = $"{product.Price:N0} VND";
                    StockQuantity = product.StockQuantity;
                    ProductUnit = product.Unit;
                    CategoryName = product.CategoryName;
                    IsProductFound = true;
                    IsProductNotFound = false;
                }
                else
                {
                    IsProductFound = false;
                    IsProductNotFound = true;
                    StatusMessage = $"Không t?m th?y s?n ph?m v?i m?: {code}";
                }
            });
        }

        private void ResetScan()
        {
            IsProductFound = false;
            IsProductNotFound = false;
            ProductName = null;
            ProductId = null;
            ProductPrice = 0;
            FormattedPrice = null;
            StockQuantity = 0;
            ProductUnit = null;
            CategoryName = null;
            LastScannedCode = null;
            StatusMessage = "Ðýa m? QR/Barcode vào khung h?nh ð? quét...";

            // Resume scanning
            _isScanning = true;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _isScanning = false;
                    StopCamera();
                    _dbContext?.Dispose();
                }
                _disposed = true;
            }
        }

        ~PriceCheckVM()
        {
            Dispose(false);
        }
        #endregion
    }

    #region Helper Classes
    public class CameraDevice
    {
        public string Name { get; set; } = string.Empty;
        public string MonikerString { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
    #endregion
}
