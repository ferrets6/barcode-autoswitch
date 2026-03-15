using BarcodeAutoSwitch.UI.ViewModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BarcodeAutoSwitch.UI.Converters;

/// <summary>Maps <see cref="PortTestResult"/> to the correct indicator GIF.</summary>
public class PortTestResultToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PortTestResult result) return null;

        string path = result switch
        {
            PortTestResult.Ok     => "/Resources/portOk.gif",
            PortTestResult.Failed => "/Resources/portKo.gif",
            _                     => "/Resources/checkPortBarcode.gif"
        };

        return new BitmapImage(new Uri(path, UriKind.Relative));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
