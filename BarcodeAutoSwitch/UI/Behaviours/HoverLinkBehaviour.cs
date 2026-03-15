using CefSharp.Wpf;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace BarcodeAutoSwitch.UI.Behaviours;

public class HoverLinkBehaviour : Behavior<ChromiumWebBrowser>
{
    public static readonly DependencyProperty HoverLinkProperty =
        DependencyProperty.Register(nameof(HoverLink), typeof(string), typeof(HoverLinkBehaviour),
            new PropertyMetadata(string.Empty));

    public string HoverLink
    {
        get => (string)GetValue(HoverLinkProperty);
        set => SetValue(HoverLinkProperty, value);
    }

    protected override void OnAttached() =>
        AssociatedObject.StatusMessage += OnStatusMessageChanged;

    protected override void OnDetaching() =>
        AssociatedObject.StatusMessage -= OnStatusMessageChanged;

    private void OnStatusMessageChanged(object sender, CefSharp.StatusMessageEventArgs e)
    {
        if (sender is ChromiumWebBrowser browser)
            browser.Dispatcher.BeginInvoke(() => HoverLink = e.Value);
    }
}
