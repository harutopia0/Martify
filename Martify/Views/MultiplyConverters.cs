using System;
using System.Globalization;
using System.Windows.Data;

namespace Martify.Views
{
    /// <summary>
    /// Converter to multiply two values (Quantity * UnitPrice)
    /// </summary>
    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0m;

            try
            {
                // Handle both int and decimal types
                decimal value1 = values[0] == null ? 0m : System.Convert.ToDecimal(values[0]);
                decimal value2 = values[1] == null ? 0m : System.Convert.ToDecimal(values[1]);
                return value1 * value2;
            }
            catch
            {
                return 0m;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("MultiplyConverter does not support ConvertBack");
        }
    }
}