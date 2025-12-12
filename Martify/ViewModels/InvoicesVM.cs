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
        // ... (Giữ nguyên toàn bộ các khai báo Property và Constructor cũ)
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
            try { QuestPDF.Settings.License = LicenseType.Community; } catch { }
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
                list = list.Where(x => x.InvoiceID.ToLower().Contains(k) || (x.Employee != null && x.Employee.FullName.ToLower().Contains(k))).ToList();
            }
            Invoices = new ObservableCollection<Invoice>(list);
        }

        // --- HÀM XUẤT PDF ĐÃ CHỈNH SỬA ---
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
                            page.DefaultTextStyle(x => x.FontSize(13).FontFamily("Arial").FontColor(PrimaryText));

                            // 1. CONTENT (Chứa toàn bộ nội dung, chảy từ trên xuống dưới)
                            page.Content().Column(col =>
                            {
                                // --- HEADER (Logo, Info) ---
                                col.Item().Column(headerCol =>
                                {
                                    headerCol.Item().AlignCenter().Text("Martify")
                                       .FontFamily("Fleur De Leah").FontSize(50).FontColor(PrimaryText);

                                    headerCol.Item().AlignCenter().Text("Hóa đơn thanh toán")
                                       .FontFamily("Charm").FontSize(24).FontColor(PrimaryText);

                                    headerCol.Item().PaddingBottom(15).AlignCenter().Text(invoice.InvoiceID)
                                       .FontSize(12).FontColor(SecondaryText);

                                    headerCol.Item().LineHorizontal(1).LineColor(DividerColor);

                                    headerCol.Item().PaddingVertical(10).Row(row =>
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

                                    headerCol.Item().PaddingBottom(5).LineHorizontal(1).LineColor(DividerColor);
                                });

                                // --- BẢNG SẢN PHẨM ---
                                col.Item().PaddingVertical(10).Table(table =>
                                {
                                    // Định nghĩa cột
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2f);   // Sản phẩm
                                        columns.RelativeColumn(1f);   // Đơn giá
                                        columns.RelativeColumn(0.8f); // SL
                                        columns.RelativeColumn(1.2f); // Thành tiền
                                    });


                                    // Hàng 1: Tiêu đề cột
                                    table.Cell().Element(HeaderStyle).Text("Sản phẩm");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Đơn giá");
                                    table.Cell().Element(HeaderStyle).AlignCenter().Text("SL");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Thành tiền");

                                    static IContainer HeaderStyle(IContainer container)
                                    {
                                        return container.PaddingVertical(5).DefaultTextStyle(x => x.Bold());
                                    }

                                    // Các hàng tiếp theo: Dữ liệu
                                    foreach (var item in invoice.InvoiceDetails)
                                    {
                                        table.Cell().Element(CellStyle).Column(c =>
                                        {
                                            c.Item().Text(item.Product?.ProductName ?? "SP đã xóa").SemiBold().FontColor(PrimaryText);
                                            c.Item().Text(item.Product?.ProductID ?? "").FontSize(10).FontColor(SecondaryText);
                                        });

                                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.SalePrice:N0}").FontColor(SecondaryText);
                                        table.Cell().Element(CellStyle).AlignCenter().Text($"{item.Quantity}").FontColor(SecondaryText);
                                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:N0}").FontColor(PrimaryText);

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.PaddingVertical(8).BorderBottom(1).BorderColor("#F5F5F5");
                                        }
                                    }
                                });

                                // --- TỔNG TIỀN (Chỉ hiện 1 lần cuối cùng) ---
                                // Dùng ShowEntire để không bị ngắt quãng
                                col.Item().ShowEntire().Column(footerCol =>
                                {
                                    footerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(DividerColor);

                                    footerCol.Item().PaddingTop(10).Row(row =>
                                    {
                                        row.RelativeItem().Text("Tổng tiền:").FontSize(14).Bold().FontColor(PrimaryText).AlignLeft();
                                        row.RelativeItem().Text($"{invoice.TotalAmount:N0} VND")
                                           .FontSize(18).Bold().FontColor(GreenColor).AlignRight();
                                    });

                                    footerCol.Item().PaddingTop(20).AlignCenter().Text("Cảm ơn quý khách đã mua hàng!").FontSize(10).Italic().FontColor(SecondaryText);
                                });
                            });

                            // Footer thật (chỉ dùng để đánh số trang ở dưới cùng tờ giấy)
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Trang ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
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