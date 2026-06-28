using System.Windows;

namespace FamilyGuard.UI.Views;

public partial class SettingsWindow : Window
{
    private bool _unlocked;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Wire to ISettingsRepository.VerifyPin + PinRateLimiter
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            PinError.Text = "Please enter a PIN.";
            PinError.Visibility = Visibility.Visible;
            return;
        }

        // Placeholder: accept any PIN for now
        _unlocked = true;
        PinPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
        SaveButton.Visibility = Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_unlocked) return;

        // TODO: Wire to ISettingsRepository.Save
        MessageBox.Show("Settings saved.", "DAD", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
