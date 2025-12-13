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

        public DashboardVM()
        {
            _dbContext = new MartifyDbContext();
            DailyRevenues = new ObservableCollection<DailyRevenueViewModel>();
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
}
