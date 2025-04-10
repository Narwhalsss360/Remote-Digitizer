using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace Common
{
    [ValueConversion(typeof(Border), typeof(Rect))]
    public class BorderToSingleRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Border border)
                throw new ArgumentException("value my be of type Border", nameof(value));
            return border.CornerRadius.TopLeft;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("One way conversion only.");
        }
    }
}
