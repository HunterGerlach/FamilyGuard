using System.Windows;
using FamilyGuard.UI.ViewModels;
using FamilyGuard.UI.Views;
using H.NotifyIcon;

namespace FamilyGuard.UI;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\FamilyGuard.UI";
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private TaskbarIcon? _trayIcon;
    private TrayViewModel? _trayViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            Shutdown();
            return;
        }

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
        if (_ownsSingleInstanceMutex)
            _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
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
            "DAD — Digital Activity Defender\n\n" +
            "A transparent family computer guidance app.\n" +
            "https://github.com/HunterGerlach/FamilyGuard",
            "About DAD",
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
