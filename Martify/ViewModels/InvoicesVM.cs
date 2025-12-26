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
        // ... (Giữ nguyên toàn bộ các khai báo Property cũ)
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
        public ICommand CloseDetailsCommand { get; set; }

        
        public ICommand OpenDetailsCommand { get; set; }
        public ICommand ExportPDFCommand { get; set; }

        public InvoicesVM()
        {
            // Cấu hình License QuestPDF (Community)
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

            CloseDetailsCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                IsDetailsPanelOpen = false;
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
                LoadFont("FleurDeleah-Regular.ttf");
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

            // Logic phân quyền
            var currentAcc = DataProvider.Ins.CurrentAccount;
            if (currentAcc != null && currentAcc.Role == 1)
            {
                query = query.Where(x => x.EmployeeID == currentAcc.EmployeeID);
            }

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

        // --- HÀM XUẤT PDF (ĐÃ CẬP NHẬT THEO FORMAT 80MM LIÊN TỤC CỦA PRINTERVM) ---
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
                            // --- CẤU HÌNH KHỔ GIẤY 80mm LIÊN TỤC (Giống PrinterVM) ---
                            // Dùng ContinuousSize để không bị ngắt trang
                            page.ContinuousSize(80, Unit.Millimetre);
                            page.Margin(5, Unit.Millimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(PrimaryText));

                            // 1. CONTENT
                            page.Content().Column(col =>
                            {
                                // --- HEADER ---
                                col.Item().Column(headerCol =>
                                {
                                    headerCol.Item().AlignCenter().Text("Martify")
                                       .FontFamily("Fleur De Leah").FontSize(30).FontColor(PrimaryText);

                                    headerCol.Item().AlignCenter().Text("Hóa đơn thanh toán")
                                       .FontFamily("Charm").FontSize(14).FontColor(PrimaryText);

                                    headerCol.Item().PaddingBottom(10).AlignCenter().Text(invoice.InvoiceID)
                                       .FontSize(8).FontColor(SecondaryText);

                                    headerCol.Item().LineHorizontal(1).LineColor(DividerColor);

                                    headerCol.Item().PaddingVertical(5).Row(row =>
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text("Ngày tạo:").FontSize(7).FontColor(SecondaryText);
                                            c.Item().Text($"{invoice.CreatedDate:dd/MM/yyyy HH:mm}").FontSize(8);
                                        });

                                        row.RelativeItem().AlignRight().Column(c =>
                                        {
                                            c.Item().AlignRight().Text("Nhân viên:").FontSize(7).FontColor(SecondaryText);
                                            c.Item().AlignRight().Text(invoice.Employee?.FullName ?? "N/A").FontSize(8);
                                        });
                                    });

                                    headerCol.Item().PaddingBottom(5).LineHorizontal(1).LineColor(DividerColor);
                                });

                                // --- BẢNG SẢN PHẨM (4 Cột) ---
                                col.Item().PaddingVertical(5).Table(table =>
                                {
                                    // Định nghĩa cột: Tên (Rộng) | Giá | SL | Thành tiền
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2f);   // Sản phẩm
                                        columns.RelativeColumn(1f);   // Đơn giá
                                        columns.RelativeColumn(0.7f); // SL
                                        columns.RelativeColumn(1.3f); // Thành tiền
                                    });

                                    // Header bảng
                                    table.Cell().Element(HeaderStyle).Text("Sản phẩm");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Đơn giá");
                                    table.Cell().Element(HeaderStyle).AlignCenter().Text("SL");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Thành tiền");

                                    static IContainer HeaderStyle(IContainer container)
                                    {
                                        return container.PaddingVertical(2).DefaultTextStyle(x => x);
                                    }

                                    // Dữ liệu
                                    foreach (var item in invoice.InvoiceDetails)
                                    {
                                        // Dòng 1: Tên sản phẩm (Gộp cột để hiển thị đầy đủ)
                                        table.Cell().ColumnSpan(4).Element(NameCellStyle).Text(item.Product?.ProductName ?? "SP đã xóa").SemiBold().FontColor(PrimaryText);

                                        // Dòng 2: Mã - Giá - SL - Tổng
                                        table.Cell().Element(CellStyle).Text(item.Product?.ProductID ?? "").FontSize(7).FontColor(SecondaryText);
                                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.SalePrice:N0}").FontColor(SecondaryText);
                                        table.Cell().Element(CellStyle).AlignCenter().Text($"{item.Quantity}").FontColor(SecondaryText);
                                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:N0}").FontColor(PrimaryText);

                                        static IContainer NameCellStyle(IContainer container)
                                        {
                                            return container.PaddingTop(4);
                                        }

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container.PaddingBottom(4).BorderBottom(1).BorderColor("#F5F5F5");
                                        }
                                    }
                                });

                                // --- TỔNG TIỀN ---
                                col.Item().ShowEntire().Column(footerCol =>
                                {
                                    footerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(DividerColor);

                                    footerCol.Item().PaddingTop(5).Row(row =>
                                    {
                                        row.RelativeItem().Text("Tổng tiền: ").FontSize(10).Bold().FontColor(PrimaryText).AlignLeft();
                                        row.RelativeItem().Text($"{invoice.TotalAmount:N0} VND")
                                           .FontSize(12).Bold().FontColor(GreenColor).AlignRight();
                                    });

                                    footerCol.Item().PaddingTop(15).AlignCenter().Text("Cảm ơn quý khách!").FontSize(8).Italic().FontColor(SecondaryText);

                                    // Footer nằm luôn trong luồng nội dung vì dùng ContinuousSize
                                    footerCol.Item().PaddingTop(2).AlignCenter().Text("Hẹn gặp lại").FontSize(8).Italic().FontColor(SecondaryText);
                                });
                            });
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}