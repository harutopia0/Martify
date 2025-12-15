using Martify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class PrinterVM : BaseVM
    {
        private Invoice _invoice;
        public Invoice CurrentInvoice
        {
            get => _invoice;
            set { _invoice = value; OnPropertyChanged(); }
        }

        public ICommand CloseCommand { get; set; }
        public ICommand SavePdfCommand { get; set; }

        public PrinterVM(Invoice invoice)
        {
            try { QuestPDF.Settings.License = LicenseType.Community; } catch { }

            RegisterProjectFonts();

            CurrentInvoice = invoice;
            CloseCommand = new RelayCommand<Window>((w) => w != null, (w) => w.Close());
            SavePdfCommand = new RelayCommand<object>((p) => true, (p) => ExportToPdf());
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

        private void ExportToPdf()
        {
            try
            {
                var invoice = DataProvider.Ins.DB.Invoices
                    .Include(x => x.Employee)
                    .Include(x => x.InvoiceDetails)
                    .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .FirstOrDefault(x => x.InvoiceID == CurrentInvoice.InvoiceID);

                if (invoice == null) return;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    FileName = $"HoaDon_{invoice.InvoiceID}_{DateTime.Now:yyyyMMddHHmmss}.pdf"
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
                            // --- THAY ĐỔI QUAN TRỌNG Ở ĐÂY ---
                            // Sử dụng ContinuousSize thay vì Size
                            // Tham số: Chiều rộng (80mm), Đơn vị
                            // PDF sẽ tự động dài ra theo nội dung, không bao giờ ngắt trang
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

                                // --- BẢNG SẢN PHẨM ---
                                col.Item().PaddingVertical(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2f);   // Sản phẩm
                                        columns.RelativeColumn(1f);   // Đơn giá
                                        columns.RelativeColumn(0.7f); // SL
                                        columns.RelativeColumn(1.3f); // Thành tiền
                                    });

                                    // Header
                                    table.Cell().Element(HeaderStyle).Text("Sản phẩm");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Đơn giá");
                                    table.Cell().Element(HeaderStyle).AlignCenter().Text("SL");
                                    table.Cell().Element(HeaderStyle).AlignRight().Text("Thành tiền");

                                    static IContainer HeaderStyle(IContainer container)
                                    {
                                        return container.PaddingVertical(2).DefaultTextStyle(x => x);
                                    }

                                    // Rows
                                    foreach (var item in invoice.InvoiceDetails)
                                    {
                                        // Tên SP
                                        table.Cell().ColumnSpan(4).Element(NameCellStyle).Text(item.Product?.ProductName ?? "SP đã xóa").SemiBold().FontColor(PrimaryText);

                                        // Chi tiết
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
                                        row.RelativeItem().Text("Tổng tiền:").FontSize(10).Bold().FontColor(PrimaryText).AlignLeft();
                                        row.RelativeItem().Text($"{invoice.TotalAmount:N0} VND")
                                           .FontSize(12).Bold().FontColor(GreenColor).AlignRight();
                                    });

                                    footerCol.Item().PaddingTop(15).AlignCenter().Text("Cảm ơn quý khách!").FontSize(8).Italic().FontColor(SecondaryText);

                                    // Footer nằm luôn trong luồng nội dung (vì trang là vô tận)
                                    footerCol.Item().PaddingTop(2).AlignCenter().Text("Hẹn gặp lại").FontSize(8).Italic().FontColor(SecondaryText);
                                });
                            });

                            // KHÔNG CẦN page.Footer() VÌ LÀ CONTINUOUS ROLL
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