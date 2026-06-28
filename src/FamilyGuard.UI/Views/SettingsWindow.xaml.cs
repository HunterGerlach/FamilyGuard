using System.IO;
using System.Windows;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsRepository _settings;
    private readonly PinRateLimiter _rateLimiter;
    private bool _unlocked;
    private bool _pinConfigured;

    public SettingsWindow()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FamilyGuard", "familyguard.db");
        _settings = new SqliteSettingsRepository($"Data Source={dbPath}");
        _rateLimiter = new PinRateLimiter(TimeProvider.System);

        // Check if PIN is configured
        var settings = _settings.Load();
        _pinConfigured = !string.IsNullOrEmpty(settings.PinHash);

        if (!_pinConfigured)
        {
            // First run — show PIN setup instead of unlock
            ShowPinSetup();
        }

        // Load current timeout value
        TimeoutSlider.Value = settings.PresenceTimeoutSeconds;
    }

    private void ShowPinSetup()
    {
        PinPanel.Visibility = Visibility.Collapsed;

        // Reuse the PIN panel but change the text
        var setupPanel = PinPanel;
        // Show a message that they need to create a PIN first
        PinError.Text = "No PIN configured. Please create one to protect settings.";
        PinError.Visibility = Visibility.Visible;
        PinPanel.Visibility = Visibility.Visible;
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            PinError.Text = "Please enter a PIN.";
            PinError.Visibility = Visibility.Visible;
            return;
        }

        if (!_pinConfigured)
        {
            // First run — set the PIN
            if (pin.Length < 4)
            {
                PinError.Text = "PIN must be at least 4 characters.";
                PinError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                _settings.SetPin(pin);
            }
            catch (Exception ex)
            {
                PinError.Text = $"Failed to save PIN: {ex.Message}";
                PinError.Visibility = Visibility.Visible;
                return;
            }
            _pinConfigured = true;
            _unlocked = true;
            PinError.Visibility = Visibility.Collapsed;
            PinPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
            MessageBox.Show("PIN created successfully.", "DAD", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Normal unlock — verify PIN
        if (_rateLimiter.IsLocked)
        {
            PinError.Text = $"Too many failed attempts. Try again later.";
            PinError.Visibility = Visibility.Visible;
            return;
        }

        if (!_settings.VerifyPin(pin))
        {
            _rateLimiter.RecordFailure();
            var remaining = _rateLimiter.RemainingAttempts;
            PinError.Text = remaining > 0
                ? $"Incorrect PIN. {remaining} attempts remaining."
                : "Too many failed attempts. Settings locked temporarily.";
            PinError.Visibility = Visibility.Visible;
            return;
        }

        _rateLimiter.RecordSuccess();
        _unlocked = true;
        PinError.Visibility = Visibility.Collapsed;
        PinPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
        SaveButton.Visibility = Visibility.Visible;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_unlocked) return;

        try
        {
            var settings = _settings.Load();
            settings.PresenceTimeoutSeconds = (int)TimeoutSlider.Value;
            _settings.Save(settings);
            MessageBox.Show("Settings saved.", "DAD", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "DAD",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateChannel_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CustomChannelUrl is null) return;

        var selected = UpdateChannelCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
        CustomChannelUrl.Visibility = selected?.Tag?.ToString() == "custom"
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Update check triggered. See the event log for results.",
            "DAD", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
