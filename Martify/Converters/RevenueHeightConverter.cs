using System;
using System.Globalization;
using System.Windows.Data;

namespace Martify.Converters
{
    public class RevenueHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return 2.0;

            try
            {
                decimal revenue = System.Convert.ToDecimal(values[0]);
                decimal maxRevenue = System.Convert.ToDecimal(values[1]);

                if (maxRevenue == 0)
                    return 2.0;

                double percentage = (double)(revenue / maxRevenue);
                double height = percentage * 120;

                return Math.Max(height, 2.0);
            }
            catch
            {
                return 2.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

