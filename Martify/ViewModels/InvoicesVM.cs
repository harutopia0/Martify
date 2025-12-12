using Martify.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Win32;
// Thư viện QuestPDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace Martify.ViewModels
{
    public class InvoicesVM : BaseVM
    {
        // ... (Giữ nguyên các Property cũ: Invoices, SelectedInvoice, Filter...)
        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices { get => _invoices; set { _invoices = value; OnPropertyChanged(); } }

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice { get => _selectedInvoice; set { _selectedInvoice = value; OnPropertyChanged(); } }

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen { get => _isDetailsPanelOpen; set { _isDetailsPanelOpen = value; OnPropertyChanged(); } }

        private string _keyword;
        public string Keyword { get => _keyword; set { _keyword = value; OnPropertyChanged(); LoadList(); } }

        private int? _selectedMonth;
        public int? SelectedMonth { get => _selectedMonth; set { _selectedMonth = value; OnPropertyChanged(); LoadList(); } }

        private int? _selectedYear;
        public int? SelectedYear { get => _selectedYear; set { _selectedYear = value; OnPropertyChanged(); LoadList(); } }

        public ObservableCollection<int> Months { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> Years { get; set; } = new ObservableCollection<int>();

        public ICommand ClearFilterCommand { get; set; }
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ExportPDFCommand { get; set; }

        public InvoicesVM()
        {
            // 1. Cấu hình License
            try { QuestPDF.Settings.License = LicenseType.Community; } catch { }

            // 2. ĐĂNG KÝ FONT TỪ PROJECT (Chỉ đăng ký 2 font tiêu đề bạn dùng)
            RegisterProjectFonts();

            InitFilterData();
            LoadList();

            OpenDetailsCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                if (p is Invoice inv)
                {
                    var fullInvoice = DataProvider.Ins.DB.Invoices
                        .Include(x => x.Employee)
                        .Include(x => x.InvoiceDetails)
                        .ThenInclude(d => d.Product)
                        .AsNoTracking()
                        .FirstOrDefault(x => x.InvoiceID == inv.InvoiceID);

                    SelectedInvoice = fullInvoice;
                    IsDetailsPanelOpen = true;
                }
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedMonth = null;
                SelectedYear = null;
                IsDetailsPanelOpen = false;
                LoadList();
            });

            ExportPDFCommand = new RelayCommand<Invoice>((p) => p != null, (p) => ExportInvoiceToPdf(p));
        }

        // --- ĐĂNG KÝ FONT (Chỉ FleurDeLeah và Charm) ---
        private void RegisterProjectFonts()
        {
            try
            {
                void LoadFont(string fileName)
                {
                    var uri = new Uri($"pack://application:,,,/Martify;component/Resources/Fonts/{fileName}");
                    var stream = Application.GetResourceStream(uri).Stream;
                    FontManager.RegisterFont(stream);
                }

                // BỎ Rubik, chỉ load 2 font này
                LoadFont("FleurDeLeah-Regular.ttf");
                LoadFont("Charm-Regular.ttf");
            }
            catch { }
        }

        void InitFilterData()
        {
            Months.Clear(); for (int i = 1; i <= 12; i++) Months.Add(i);
            Years.Clear();
            var dbYears = DataProvider.Ins.DB.Invoices.Select(x => x.CreatedDate.Year).Distinct().OrderByDescending(y => y).ToList();
            foreach (var y in dbYears) Years.Add(y);
            if (Years.Count == 0) Years.Add(DateTime.Now.Year);
        }

        void LoadList()
        {
            var query = DataProvider.Ins.DB.Invoices.Include(x => x.Employee).AsNoTracking().AsQueryable();

            if (SelectedMonth.HasValue) query = query.Where(x => x.CreatedDate.Month == SelectedMonth.Value);
            if (SelectedYear.HasValue) query = query.Where(x => x.CreatedDate.Year == SelectedYear.Value);

            var list = query.OrderByDescending(x => x.CreatedDate).ToList();

            if (!string.IsNullOrEmpty(Keyword))
            {
                string k = Keyword.ToLower();
                list = list.Where(x => x.InvoiceID.ToLower().Contains(k) ||
                                       (x.Employee != null && x.Employee.FullName.ToLower().Contains(k))).ToList();
            }
            Invoices = new ObservableCollection<Invoice>(list);
        }

        // --- HÀM XUẤT PDF ---
        private void ExportInvoiceToPdf(Invoice simpleInvoice)
        {
            try
            {
                var invoice = DataProvider.Ins.DB.Invoices
                    .Include(x => x.Employee)
                    .Include(x => x.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .FirstOrDefault(x => x.InvoiceID == simpleInvoice.InvoiceID);

                if (invoice == null) return;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    FileName = $"HoaDon_{invoice.InvoiceID}_{DateTime.Now:ddMMyyyy}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Định nghĩa màu sắc giống LightTheme
                    var PrimaryText = "#333333";
                    var SecondaryText = "#666666";
                    var GreenColor = "#4CAF50";
                    var DividerColor = "#E0E0E0";

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                            page.PageColor(Colors.White);

                            // DÙNG FONT ARIAL CHO NỘI DUNG CHÍNH (Giống mặc định của WPF)
                            page.DefaultTextStyle(x => x.FontSize(13).FontFamily("Arial").FontColor(PrimaryText));

                            // === 1. HEADER ===
                            page.Header().Column(col =>
                            {
                                // Logo "Martify" - Dùng font Fleur De Leah
                                col.Item().AlignCenter().Text("Martify")
                                   .FontFamily("Fleur De Leah").FontSize(50).FontColor(PrimaryText);

                                // Tiêu đề "Hóa đơn thanh toán" - Dùng font Charm
                                col.Item().AlignCenter().Text("Hóa đơn thanh toán")
                                   .FontFamily("Charm").FontSize(24).FontColor(PrimaryText);

                                // Mã hóa đơn (Arial)
                                col.Item().PaddingBottom(15).AlignCenter().Text(invoice.InvoiceID)
                                   .FontSize(12).FontColor(SecondaryText);

                                col.Item().LineHorizontal(1).LineColor(DividerColor);

                                // Thông tin Ngày tạo & Nhân viên
                                col.Item().PaddingVertical(10).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Ngày tạo:").FontSize(10).FontColor(SecondaryText);
                                        c.Item().Text($"{invoice.CreatedDate:dd/MM/yyyy HH:mm}").FontSize(12);
                                    });

                                    row.RelativeItem().AlignRight().Column(c =>
                                    {
                                        c.Item().AlignRight().Text("Nhân viên:").FontSize(10).FontColor(SecondaryText);
                                        c.Item().AlignRight().Text(invoice.Employee?.FullName ?? "N/A").FontSize(12);
                                    });
                                });

                                col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(DividerColor);
                            });

                            // === 2. CONTENT (Bảng sản phẩm) ===
                            page.Content().PaddingVertical(10).Table(table =>
                            {
                                // Tỷ lệ cột giống XAML: 2* - 1* - 0.8* - 1.2*
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2f);
                                    columns.RelativeColumn(1f);
                                    columns.RelativeColumn(0.8f);
                                    columns.RelativeColumn(1.2f);
                                });

                                // Header Bảng
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Sản phẩm");
                                    header.Cell().Element(HeaderStyle).AlignRight().Text("Đơn giá");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("SL"); // SL căn giữa
                                    header.Cell().Element(HeaderStyle).AlignRight().Text("Thành tiền");

                                    static IContainer HeaderStyle(IContainer container)
                                    {
                                        return container.PaddingVertical(5).DefaultTextStyle(x => x.Bold());
                                    }
                                });

                                // Dữ liệu từng dòng
                                foreach (var item in invoice.InvoiceDetails)
                                {
                                    // Cột 1: Tên SP (Đậm) + Mã SP (Nhạt)
                                    table.Cell().Element(CellStyle).Column(c =>
                                    {
                                        c.Item().Text(item.Product?.ProductName ?? "SP đã xóa").SemiBold().FontColor(PrimaryText);
                                        c.Item().Text(item.Product?.ProductID ?? "").FontSize(10).FontColor(SecondaryText);
                                    });

                                    // Cột 2: Đơn giá
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.SalePrice:N0}").FontColor(SecondaryText);

                                    // Cột 3: Số lượng
                                    table.Cell().Element(CellStyle).AlignCenter().Text($"{item.Quantity}").FontColor(SecondaryText);

                                    // Cột 4: Thành tiền
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:N0}").FontColor(PrimaryText);

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.PaddingVertical(8).BorderBottom(1).BorderColor("#F5F5F5");
                                    }
                                }
                            });

                            // === 3. FOOTER ===
                            page.Footer().Column(col =>
                            {
                                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(DividerColor);

                                col.Item().PaddingTop(10).Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng tiền:").FontSize(14).Bold().FontColor(PrimaryText).AlignLeft();

                                    row.RelativeItem().Text($"{invoice.TotalAmount:N0} VND")
                                       .FontSize(18).Bold().FontColor(GreenColor).AlignRight();
                                });

                                col.Item().PaddingTop(20).AlignCenter().Text("Cảm ơn quý khách đã mua hàng!").FontSize(10).Italic().FontColor(SecondaryText);
                            });
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName);

                    MessageBox.Show("Xuất hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}