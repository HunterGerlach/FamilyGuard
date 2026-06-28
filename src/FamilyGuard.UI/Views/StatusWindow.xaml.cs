using System.IO;
using System.Windows;
using System.Windows.Threading;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Views;

public partial class StatusWindow : Window
{
    private readonly SqliteSettingsRepository? _settings;
    private readonly DispatcherTimer _refreshTimer;

    public StatusWindow()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FamilyGuard", "familyguard.db");

        if (File.Exists(dbPath))
        {
            _settings = new SqliteSettingsRepository($"Data Source={dbPath};Mode=ReadOnly");
        }

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => RefreshStatus();
        _refreshTimer.Start();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (DataContext is not StatusViewModel vm || _settings is null)
            return;

        try
        {
            var state = _settings.LoadAgentState();
            if (state is null)
            {
                vm.MonitoringStatus = "Waiting for agent...";
                return;
            }

            vm.MonitoringStatus = "Active";
            vm.PresenceState = state.PresenceState;
            vm.MicState = state.MicMuted ? "Muted" : "Unmuted";
            vm.MicDeviceName = state.MicDeviceName;
            vm.InactiveSeconds = state.InactiveSeconds;
            vm.LastPolicyAction = state.LastAction;
        }
        catch
        {
            // Database may be locked — silently retry next tick
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Stop();
        Close();
    }
}
