using System.Globalization;
using System.Windows.Data;

namespace BarcodeAutoSwitch.UI.Converters;

public class TitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        $"BarcodeAutoSwitch - {value ?? "No Title"}";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
