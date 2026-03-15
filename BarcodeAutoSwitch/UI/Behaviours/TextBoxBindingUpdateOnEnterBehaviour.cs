using Microsoft.Xaml.Behaviors;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfKey = System.Windows.Input.Key;

namespace BarcodeAutoSwitch.UI.Behaviours;

public class TextBoxBindingUpdateOnEnterBehaviour : Behavior<WpfTextBox>
{
    protected override void OnAttached() =>
        AssociatedObject.KeyDown += OnKeyDown;

    protected override void OnDetaching() =>
        AssociatedObject.KeyDown -= OnKeyDown;

    private void OnKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.Enter && sender is WpfTextBox tb)
            tb.GetBindingExpression(WpfTextBox.TextProperty)?.UpdateSource();
    }
}
