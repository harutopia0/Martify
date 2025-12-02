using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Martify.Converters // Nhớ check namespace cho đúng folder
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Lấy chuỗi đường dẫn từ Database
            string relativePath = value as string;

            // Nếu DB null hoặc rỗng -> Trả về null để XAML tự hiện ảnh Anonymous
            if (string.IsNullOrEmpty(relativePath))
                return null;

            // 2. Tạo đường dẫn tuyệt đối (tính từ file .exe đang chạy)
            // VD: C:\Projects\Martify\bin\Debug\net8.0-windows\ + Images\administrator.png
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(baseDir, relativePath);

            // 3. Kiểm tra file có tồn tại thật không?
            if (File.Exists(fullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);

                    // CacheOption = OnLoad: Load xong nhả file ra ngay, để sau này có xóa/sửa file cũng không bị lỗi "File đang được sử dụng"
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Tránh cache cũ


                    bitmap.DecodePixelWidth = 150;

                    bitmap.EndInit();

                    bitmap.Freeze(); // Quan trọng: Tăng tốc render UI

                    return bitmap; // Trả về ảnh thật
                }
                catch
                {
                    return null; // Lỗi đọc file -> Trả về null -> Hiện Anonymous
                }
            }

            // 4. Nếu file không tồn tại (đường dẫn sai, hoặc file bị xóa)
            // -> Trả về null để XAML tự hiện ảnh Anonymous
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}