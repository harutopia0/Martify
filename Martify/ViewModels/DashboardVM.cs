using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Martify.ViewModels
{
    class DashboardVM : BaseVM
    {
        private MartifyDbContext _dbContext;

        private decimal _giaTriDoanhThu;
        public decimal GiaTriDoanhThu
        {
            get => _giaTriDoanhThu;
            set
            {
                _giaTriDoanhThu = value;
                OnPropertyChanged();
            }
        }

        private decimal _tyLeThayDoi;
        public decimal TyLeThayDoi
        {
            get => _tyLeThayDoi;
            set
            {
                _tyLeThayDoi = value;
                OnPropertyChanged();
            }
        }

        private bool _isRevenueIncreased;
        public bool IsRevenueIncreased
        {
            get => _isRevenueIncreased;
            set
            {
                _isRevenueIncreased = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DailyRevenueViewModel> _dailyRevenues;
        public ObservableCollection<DailyRevenueViewModel> DailyRevenues
        {
            get => _dailyRevenues;
            set
            {
                _dailyRevenues = value;
                OnPropertyChanged();
            }
        }

        private decimal _maxDailyRevenue;
        public decimal MaxDailyRevenue
        {
            get => _maxDailyRevenue;
            set
            {
                _maxDailyRevenue = value;
                OnPropertyChanged();
            }
        }

        private int _tongSoSanPham;
        public int TongSoSanPham
        {
            get => _tongSoSanPham;
            set
            {
                _tongSoSanPham = value;
                OnPropertyChanged();
            }
        }

        private int _tongSoDonHang;
        public int TongSoDonHang
        {
            get => _tongSoDonHang;
            set
            {
                _tongSoDonHang = value;
                OnPropertyChanged();
            }
        }

        private int _tongSoNhanVien;
        public int TongSoNhanVien
        {
            get => _tongSoNhanVien;
            set
            {
                _tongSoNhanVien = value;
                OnPropertyChanged();
            }
        }

        private int _soSanPhamConLai;
        public int SoSanPhamConLai
        {
            get => _soSanPhamConLai;
            set
            {
                _soSanPhamConLai = value;
                OnPropertyChanged();
            }
        }

        // Số sản phẩm sắp hết hàng (Low Stock Alert)
        private int _soSanPhamSapHet;
        public int SoSanPhamSapHet
        {
            get => _soSanPhamSapHet;
            set
            {
                _soSanPhamSapHet = value;
                OnPropertyChanged();
            }
        }

        // Số sản phẩm hết hàng (Out of Stock)
        private int _soSanPhamHetHang;
        public int SoSanPhamHetHang
        {
            get => _soSanPhamHetHang;
            set
            {
                _soSanPhamHetHang = value;
                OnPropertyChanged();
            }
        }

        // Top 5 high-value invoices
        private ObservableCollection<HighValueInvoiceViewModel> _topInvoices;
        public ObservableCollection<HighValueInvoiceViewModel> TopInvoices
        {
            get => _topInvoices;
            set
            {
                _topInvoices = value;
                OnPropertyChanged();
            }
        }

        // Top 5 best selling products
        private ObservableCollection<TopProductViewModel> _topProducts;
        public ObservableCollection<TopProductViewModel> TopProducts
        {
            get => _topProducts;
            set
            {
                _topProducts = value;
                OnPropertyChanged();
            }
        }

        public DashboardVM()
        {
            _dbContext = new MartifyDbContext();
            DailyRevenues = new ObservableCollection<DailyRevenueViewModel>();
            TopInvoices = new ObservableCollection<HighValueInvoiceViewModel>();
            TopProducts = new ObservableCollection<TopProductViewModel>();
            LoadDashboardData();
        }

        /// <summary>
        /// Điều hướng đến trang Products với bộ lọc cảnh báo tồn kho
        /// </summary>
        public void NavigateToInventoryAlert(InventoryAlertType alertType)
        {
            // Lấy NavigationVM từ MainVM
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow?.DataContext is MainVM mainVM)
            {
                mainVM.Navigation.NavigateToProductsWithAlert(alertType);
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                CalculateWeeklyRevenue();
                LoadQuickOverview();
                LoadTopInvoices();
                LoadTopProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu dashboard: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateWeeklyRevenue()
        {
            var today = DateTime.Today;
            
            // Get all invoices from the last 14 days in a single query for better performance
            var startDate = today.AddDays(-13);
            var endDate = today.AddDays(1);
            
            var allInvoices = _dbContext.Invoices
                .Where(inv => inv.CreatedDate >= startDate && inv.CreatedDate < endDate)
                .Select(inv => new { inv.CreatedDate, inv.TotalAmount })
                .AsEnumerable()
                .ToList();

            var last7Days = new List<DailyRevenueViewModel>();

            // Calculate revenue for last 7 days
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var startOfDay = date.Date;
                var endOfDay = date.Date.AddDays(1);

                var dailyRevenue = allInvoices
                    .Where(inv => inv.CreatedDate >= startOfDay && inv.CreatedDate < endOfDay)
                    .Sum(inv => inv.TotalAmount);

                last7Days.Add(new DailyRevenueViewModel
                {
                    Date = date,
                    Revenue = dailyRevenue
                });
            }

            // Calculate revenue for previous 7 days (8-14 days ago) from cached data
            var previous7DaysRevenue = allInvoices
                .Where(inv => inv.CreatedDate >= today.AddDays(-13) && inv.CreatedDate < today.AddDays(-6))
                .Sum(inv => inv.TotalAmount);

            var maxRevenue = last7Days.Max(d => d.Revenue);
            MaxDailyRevenue = maxRevenue > 0 ? maxRevenue : 1;

            var totalRevenue = last7Days.Sum(d => d.Revenue);
            GiaTriDoanhThu = totalRevenue;

            // Calculate percentage change
            if (previous7DaysRevenue > 0)
            {
                TyLeThayDoi = Math.Round(((totalRevenue - previous7DaysRevenue) / previous7DaysRevenue) * 100, 1);
                IsRevenueIncreased = totalRevenue >= previous7DaysRevenue;
            }
            else
            {
                TyLeThayDoi = totalRevenue > 0 ? 100 : 0;
                IsRevenueIncreased = totalRevenue > 0;
            }

            // Set previous and next day comparisons
            for (int i = 0; i < last7Days.Count; i++)
            {
                last7Days[i].MaxRevenue = MaxDailyRevenue;
                last7Days[i].TotalWeekRevenue = totalRevenue;

                // Set previous day data
                if (i > 0)
                {
                    last7Days[i].HasPreviousDay = true;
                    last7Days[i].PreviousDayRevenue = last7Days[i - 1].Revenue;
                }
                else
                {
                    last7Days[i].HasPreviousDay = false;
                }

                // Set next day data
                if (i < last7Days.Count - 1)
                {
                    last7Days[i].HasNextDay = true;
                    last7Days[i].NextDayRevenue = last7Days[i + 1].Revenue;
                }
                else
                {
                    last7Days[i].HasNextDay = false;
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DailyRevenues.Clear();
                foreach (var day in last7Days)
                {
                    DailyRevenues.Add(day);
                }
            });
        }

        private void LoadQuickOverview()
        {
            // Tổng số lượng
            TongSoSanPham = _dbContext.Products.Count();
            TongSoDonHang = _dbContext.Invoices.Count();
            TongSoNhanVien = _dbContext.Employees.Count();
            SoSanPhamConLai = _dbContext.Products.AsEnumerable().Sum(p => p.StockQuantity);

            // Cảnh báo sắp hết hàng (Low Stock Alert)
            // Logic: Sản phẩm có số lượng tồn <= 10 (hoặc bạn có thể thêm trường MinStockLevel vào Product)
            const int MIN_STOCK_THRESHOLD = 10;
            SoSanPhamSapHet = _dbContext.Products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= MIN_STOCK_THRESHOLD);

            // Cảnh báo hết hàng (Out of Stock)
            // Logic: Sản phẩm có số lượng tồn kho = 0
            SoSanPhamHetHang = _dbContext.Products
                .Count(p => p.StockQuantity == 0);
        }

        private void LoadTopInvoices()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6); // Last 7 days including today

            // Get top 5 invoices from the last 7 days
            // First get all invoices, then order and take in memory to avoid SQLite decimal ordering issue
            var topInvoices = _dbContext.Invoices
                .Include(i => i.Employee)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Product)
                .Where(i => i.CreatedDate >= weekStart && i.CreatedDate <= today.AddDays(1))
                .AsEnumerable() // Execute query and bring to memory
                .OrderByDescending(i => i.TotalAmount)
                .Take(5)
                .Select(i =>
                {
                    var allDetails = i.InvoiceDetails.ToList();
                    var top5Details = allDetails
                        .OrderByDescending(d => d.Total)
                        .Take(5)
                        .ToList();

                    return new HighValueInvoiceViewModel
                    {
                        Rank = 0, // Will be set later
                        InvoiceID = i.InvoiceID,
                        EmployeeName = i.Employee != null ? i.Employee.FullName : "N/A",
                        CreatedDate = i.CreatedDate,
                        TotalAmount = i.TotalAmount,
                        InvoiceDetails = new ObservableCollection<InvoiceDetail>(allDetails),
                        Top5InvoiceDetails = new ObservableCollection<InvoiceDetail>(top5Details),
                        TotalProductCount = allDetails.Count
                    };
                })
                .ToList();

            // Fill with default values if less than 5
            for (int i = topInvoices.Count; i < 5; i++)
            {
                topInvoices.Add(new HighValueInvoiceViewModel
                {
                    Rank = i + 1,
                    InvoiceID = "HD00000",
                    EmployeeName = "Tên",
                    CreatedDate = DateTime.MinValue,
                    TotalAmount = 0,
                    IsDefault = true,
                    InvoiceDetails = new ObservableCollection<InvoiceDetail>(),
                    Top5InvoiceDetails = new ObservableCollection<InvoiceDetail>(),
                    TotalProductCount = 0
                });
            }

            // Set ranks
            for (int i = 0; i < topInvoices.Count; i++)
            {
                topInvoices[i].Rank = i + 1;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                TopInvoices.Clear();
                foreach (var invoice in topInvoices)
                {
                    TopInvoices.Add(invoice);
                }
            });
        }

        private void LoadTopProducts()
        {
            // Get top 5 best selling products based on total quantity sold
            var topProducts = _dbContext.InvoiceDetails
                .Include(id => id.Product)
                    .ThenInclude(p => p.Category)
                .GroupBy(id => id.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Product = g.First().Product,
                    TotalQuantity = g.Sum(id => id.Quantity)
                })
                .AsEnumerable() // Execute query and bring to memory
                .OrderByDescending(p => p.TotalQuantity)
                .Take(5)
                .Select(p => new TopProductViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.Product.ProductName,
                    QuantitySold = p.TotalQuantity,
                    ImagePath = p.Product.ImagePath,
                    Product = p.Product
                })
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                TopProducts.Clear();
                foreach (var product in topProducts)
                {
                    TopProducts.Add(product);
                }
            });
        }

        public void RefreshData()
        {
            LoadDashboardData();
        }
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal MaxRevenue { get; set; }
        public decimal TotalWeekRevenue { get; set; }
        public decimal PreviousDayRevenue { get; set; }
        public decimal NextDayRevenue { get; set; }
        public bool HasPreviousDay { get; set; }
        public bool HasNextDay { get; set; }

        public string DayLabel => Date.ToString("dd/MM");
        public string DayOfWeek => Date.ToString("ddd", new System.Globalization.CultureInfo("vi-VN"));
        public string FormattedRevenue => $"{Revenue:N0} VND";
        public string FullDate => Date.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
        
        public double PercentageOfWeek
        {
            get
            {
                if (TotalWeekRevenue == 0) return 0;
                return (double)(Revenue / TotalWeekRevenue) * 100;
            }
        }

        public string PercentageOfWeekFormatted => $"{PercentageOfWeek:F1}%";

        public decimal ComparisonWithPrevious
        {
            get
            {
                if (!HasPreviousDay || PreviousDayRevenue == 0) return 0;
                return ((Revenue - PreviousDayRevenue) / PreviousDayRevenue) * 100;
            }
        }

        public string ComparisonWithPreviousFormatted
        {
            get
            {
                if (!HasPreviousDay) return "N/A";
                var value = ComparisonWithPrevious;
                var sign = value >= 0 ? "+" : "";
                return $"{sign}{value:F1}%";
            }
        }

        public decimal ComparisonWithNext
        {
            get
            {
                if (!HasNextDay || NextDayRevenue == 0) return 0;
                return ((Revenue - NextDayRevenue) / NextDayRevenue) * 100;
            }
        }

        public string ComparisonWithNextFormatted
        {
            get
            {
                if (!HasNextDay) return "N/A";
                var value = ComparisonWithNext;
                var sign = value >= 0 ? "+" : "";
                return $"{sign}{value:F1}%";
            }
        }

        public double BarHeightPercentage
        {
            get
            {
                if (MaxRevenue == 0) return 0;
                return (double)(Revenue / MaxRevenue) * 100;
            }
        }

        public double BarHeight
        {
            get
            {
                if (MaxRevenue == 0) return 0;
                var percentage = (double)(Revenue / MaxRevenue);
                return percentage * 100;
            }
        }
    }

    public class HighValueInvoiceViewModel
    {
        public int Rank { get; set; }
        public string InvoiceID { get; set; }
        public string EmployeeName { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsDefault { get; set; }
        public ObservableCollection<InvoiceDetail> InvoiceDetails { get; set; }
        public ObservableCollection<InvoiceDetail> Top5InvoiceDetails { get; set; }
        public int TotalProductCount { get; set; }
        public bool HasMoreProducts => TotalProductCount > 5;

        public string FormattedDate => IsDefault ? "xx/xx/xxxx" : CreatedDate.ToString("dd/MM/yyyy");
        public string FullFormattedDate => IsDefault ? "xx/xx/xxxx" : CreatedDate.ToString("dddd, dd/MM/yyyy HH:mm", new System.Globalization.CultureInfo("vi-VN"));
        public string FormattedAmount => IsDefault ? "0 VND" : $"{TotalAmount:N0} VND";
        public string RemainingProductsText => $"+ {TotalProductCount - 5} sản phẩm khác";
        
        public string RankText
        {
            get
            {
                return Rank switch
                {
                    1 => "🥇 Hạng 1",
                    2 => "🥈 Hạng 2",
                    3 => "🥉 Hạng 3",
                    4 => "#4",
                    5 => "#5",
                    _ => $"#{Rank}"
                };
            }
        }
        
        public string RankColor
        {
            get
            {
                return Rank switch
                {
                    1 => "#067FF8",
                    2 => "#4CAF50",
                    3 => "#FF9800",
                    _ => "#B0BEC5"
                };
            }
        }
    }

    public class TopProductViewModel
    {
        public string ProductID { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public string ImagePath { get; set; }
        public Product Product { get; set; }

        public string FormattedQuantitySold => $"Đã bán: {QuantitySold:N0}";
        public string FormattedQuantityOnly => $"{QuantitySold:N0} sản phẩm";
        public string FormattedPrice => $"{Product?.Price:N0} VND";
        public string FormattedStockQuantity => $"{Product?.StockQuantity:N0}";
        public string CategoryName => Product?.Category?.CategoryName ?? "N/A";
        public string Unit => Product?.Unit ?? "N/A";
        
        public string PerformanceLevel
        {
            get
            {
                if (QuantitySold >= 100) return "🔥 Siêu Hot";
                if (QuantitySold >= 80) return "⭐ Rất Hot";
                if (QuantitySold >= 60) return "✨ Hot";
                if (QuantitySold >= 40) return "📈 Bán Tốt";
                return "📊 Ổn Định";
            }
        }
        
        public string BackgroundColor
        {
            get
            {
                // Assign different colors based on sales performance
                if (QuantitySold >= 100) return "#FFE0B2";
                if (QuantitySold >= 80) return "#E3F2FD";
                if (QuantitySold >= 60) return "#F3E5F5";
                if (QuantitySold >= 40) return "#FFF3E0";
                return "#E8F5E9";
            }
        }

        public string IconColor
        {
            get
            {
                if (QuantitySold >= 100) return "#FF9800";
                if (QuantitySold >= 80) return "#2196F3";
                if (QuantitySold >= 60) return "#9C27B0";
                if (QuantitySold >= 40) return "#FF6F00";
                return "#388E3C";
            }
        }
    }
}
