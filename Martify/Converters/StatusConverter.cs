using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media; // Cần thêm cái này để dùng Brushes

namespace Martify.Converters
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = value as bool? ?? false; // Mặc định false nếu null
            string param = parameter as string;

            // 1. Xử lý màu chữ (Foreground)
            if (param == "Foreground")
            {
                return status ? Brushes.Green : Brushes.Red;
            }

            // 2. Xử lý màu nền (Background) - optional nếu bạn muốn nền nhạt
            if (param == "Background")
            {
                // Màu nền nhạt cho đẹp (Xanh nhạt / Đỏ nhạt)
                return status ? new SolidColorBrush(Color.FromRgb(220, 255, 220)) : new SolidColorBrush(Color.FromRgb(255, 220, 220));
            }

            // 3. Mặc định trả về Text hiển thị
            return status ? "Đang làm việc" : "Đã nghỉ việc";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}