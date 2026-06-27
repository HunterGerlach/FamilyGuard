using System.Windows;
using FamilyGuard.UI.ViewModels;
using FamilyGuard.UI.Views;
using H.NotifyIcon;

namespace FamilyGuard.UI;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private TrayViewModel? _trayViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayViewModel = new TrayViewModel();
        _trayIcon.DataContext = _trayViewModel;

        // Start with green icon (monitoring active, present)
        _trayViewModel.UpdateState(TrayIconState.Normal);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private void ShowStatus_Click(object sender, RoutedEventArgs e)
    {
        ShowOrActivateWindow<StatusWindow>();
    }

    private void MuteNow_Click(object sender, RoutedEventArgs e)
    {
        _trayViewModel?.MuteNowCommand.Execute(null);
    }

    private void UnmuteNow_Click(object sender, RoutedEventArgs e)
    {
        _trayViewModel?.UnmuteCommand.Execute(null);
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowOrActivateWindow<SettingsWindow>();
    }

    private void ShowEventLog_Click(object sender, RoutedEventArgs e)
    {
        ShowOrActivateWindow<EventLogWindow>();
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "DaD — Digital Activity Defender\n\n" +
            "A transparent family computer guidance app.\n" +
            "https://github.com/HunterGerlach/FamilyGuard",
            "About DaD",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void ShowOrActivateWindow<T>() where T : Window, new()
    {
        var existing = Current.Windows.OfType<T>().FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var window = new T();
        window.Show();
    }
}
