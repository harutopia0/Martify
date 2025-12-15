using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Martify.Views
{
    public partial class ReceiptPrinterWindow : Window
    {
        public ReceiptPrinterWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var sb = this.FindResource("PrintAnimation") as Storyboard;
            sb?.Begin();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lấy đối tượng tờ giấy in
                var element = ReceiptTicket;

                // 1. Tính toán kích thước THẬT của nội dung (bao gồm phần bị ẩn do cuộn)
                // Measure với Infinity để lấy kích thước đầy đủ mong muốn
                element.Measure(new Size(element.ActualWidth, double.PositiveInfinity));

                // Lấy kích thước đã tính toán
                Size size = element.DesiredSize;

                // Ép Layout lại theo kích thước đầy đủ này để render
                element.Arrange(new Rect(size));
                element.UpdateLayout();

                // 2. Render ra Bitmap
                double dpi = 96;
                double scale = dpi / 96;

                RenderTargetBitmap bmp = new RenderTargetBitmap(
                    (int)(size.Width * scale),
                    (int)(size.Height * scale),
                    dpi, dpi, PixelFormats.Pbgra32);

                bmp.Render(element);

                // 3. Lưu file
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg";
                saveFileDialog.FileName = $"Receipt_{DateTime.Now:yyyyMMddHHmmss}";

                if (saveFileDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                    using (var stream = File.Create(saveFileDialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Đã lưu hóa đơn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message);
            }
        }
    }
}