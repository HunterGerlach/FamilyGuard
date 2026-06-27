using System.Windows;

namespace FamilyGuard.UI.Views;

public partial class PinSetupWindow : Window
{
    public string? Pin { get; private set; }

    public PinSetupWindow()
    {
        InitializeComponent();
        PinBox.Focus();
    }

    private void SetPin_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        var confirm = ConfirmPinBox.Password;

        if (string.IsNullOrWhiteSpace(pin))
        {
            ShowError("PIN cannot be empty.");
            return;
        }

        if (pin.Length < 4)
        {
            ShowError("PIN must be at least 4 characters.");
            return;
        }

        if (pin != confirm)
        {
            ShowError("PINs do not match.");
            return;
        }

        Pin = pin;
        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
