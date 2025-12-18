using Martify.Models;
using System;
using System.Linq;
using System.Windows;

namespace Martify
{
    public partial class App : Application
    {
        // Hàm này sẽ được gọi khi bạn muốn đổi theme
        public void SetTheme(bool isDark)
        {
            // 1. Xác định đường dẫn file theme muốn dùng
            // Đảm bảo đường dẫn này ĐÚNG với nơi bạn lưu file
            string themePath = isDark ?
                "Resources/XAML/DarkTheme.xaml" :
                "Resources/XAML/LightTheme.xaml";

            // 2. Tạo ResourceDictionary mới từ đường dẫn
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            // 3. Tìm và XÓA theme cũ đi
            // Chúng ta tìm trong danh sách MergedDictionaries cái nào là theme cũ
            var oldTheme = Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("LightTheme.xaml") ||
                 d.Source.OriginalString.Contains("DarkTheme.xaml")));

            if (oldTheme != null)
            {
                Resources.MergedDictionaries.Remove(oldTheme);
            }

            // 4. THÊM theme mới vào
            Resources.MergedDictionaries.Add(newTheme);
        }

      
    }
}