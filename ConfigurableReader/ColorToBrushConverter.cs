using System.Windows.Data;
using System.Windows.Media;

namespace ConfigurableReader;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return Binding.DoNothing;
    }
}