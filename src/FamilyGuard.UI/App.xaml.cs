using System.IO;
using System.Windows;
using FamilyGuard.UI.ViewModels;
using FamilyGuard.UI.Views;
using H.NotifyIcon;

namespace FamilyGuard.UI;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\FamilyGuard.UI";
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "FamilyGuard", "ui.log");

    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private TaskbarIcon? _trayIcon;
    private TrayViewModel? _trayViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log("UI starting");

            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _ownsSingleInstanceMutex);
            if (!_ownsSingleInstanceMutex)
            {
                Log("Another instance is already running — exiting");
                Shutdown();
                return;
            }

            base.OnStartup(e);

            Log("Creating tray icon");
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayViewModel = new TrayViewModel();
            _trayIcon.DataContext = _trayViewModel;

            // Set icon and state — must happen after DataContext is assigned
            _trayViewModel.UpdateState(TrayIconState.Normal);
            _trayIcon.IconSource = _trayViewModel.IconSource;
            _trayIcon.ToolTipText = _trayViewModel.ToolTipText;

            // Subscribe to state changes to update the icon
            _trayViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(TrayViewModel.IconSource))
                    _trayIcon.IconSource = _trayViewModel.IconSource;
                if (args.PropertyName == nameof(TrayViewModel.ToolTipText))
                    _trayIcon.ToolTipText = _trayViewModel.ToolTipText;
            };

            _trayIcon.ForceCreate();

            Log($"Tray icon created. IconSource type: {_trayViewModel.IconSource?.GetType().Name ?? "null"}");
        }
        catch (Exception ex)
        {
            Log($"FATAL: {ex}");
            MessageBox.Show(
                $"DAD failed to start:\n\n{ex.Message}\n\nSee {LogPath} for details.",
                "DAD Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log("UI shutting down");
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

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath,
                $"{DateTimeOffset.UtcNow:O} [UI] {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging failure must never crash the app
        }
    }
}
