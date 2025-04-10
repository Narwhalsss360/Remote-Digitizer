using System.CodeDom;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Common
{
    [ValueConversion(typeof(Border), typeof(Rect))]
    public class BorderToRectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Border border)
                throw new ArgumentException("value my be of type Border", nameof(value));
            return new Rect(new Size(border.ActualWidth, border.ActualHeight));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("One way conversion only.");
        }
    }
}
