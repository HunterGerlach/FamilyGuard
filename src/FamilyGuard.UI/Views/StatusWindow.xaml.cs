using System.IO;
using System.Windows;
using System.Windows.Threading;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Views;

public partial class StatusWindow : Window
{
    private readonly SqliteEventStore? _eventStore;
    private readonly DispatcherTimer _refreshTimer;

    public StatusWindow()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FamilyGuard", "familyguard.db");

        if (File.Exists(dbPath))
        {
            _eventStore = new SqliteEventStore($"Data Source={dbPath};Mode=ReadOnly");
        }

        // Refresh status every 2 seconds
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => RefreshStatus();
        _refreshTimer.Start();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (DataContext is not StatusViewModel vm || _eventStore is null)
            return;

        try
        {
            // Get latest events to determine current state
            var recentEvents = _eventStore.QueryRecent(10);

            var lastMicEvent = recentEvents.FirstOrDefault(e =>
                e.EventType is EventType.MicAutoMuted or EventType.MicManualMuted or EventType.MicManualUnmuted);

            var lastPresenceEvent = recentEvents.FirstOrDefault(e =>
                e.EventType == EventType.PresenceStateChanged);

            var serviceRunning = recentEvents.Any(e =>
                e.EventType == EventType.ServiceStarted || e.EventType == EventType.AgentStarted);

            vm.MonitoringStatus = serviceRunning ? "Active" : "Unknown";

            if (lastMicEvent is not null)
            {
                vm.MicState = lastMicEvent.EventType switch
                {
                    EventType.MicAutoMuted => "Muted (auto)",
                    EventType.MicManualMuted => "Muted (manual)",
                    EventType.MicManualUnmuted => "Unmuted",
                    _ => "Unknown"
                };
                vm.LastPolicyAction = $"{lastMicEvent.EventType} at {lastMicEvent.TimestampUtc:HH:mm:ss}";

                if (lastMicEvent.Details.TryGetValue("device_name", out var micName))
                    vm.MicDeviceName = micName;
            }

            if (lastPresenceEvent is not null && lastPresenceEvent.Details.TryGetValue("new_state", out var state))
            {
                vm.PresenceState = state;
            }
        }
        catch
        {
            // Database may be locked by the service — silently retry next tick
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Stop();
        Close();
    }
}
