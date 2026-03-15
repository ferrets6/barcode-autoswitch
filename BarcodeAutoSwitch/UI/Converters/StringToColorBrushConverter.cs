using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BarcodeAutoSwitch.UI.Converters;

/// <summary>Converts a color name string (e.g. "Green") to a <see cref="SolidColorBrush"/>.</summary>
public class StringToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorName)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorName);
            return new SolidColorBrush(color);
        }
        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
