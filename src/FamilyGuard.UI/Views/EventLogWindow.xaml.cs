using System.IO;
using System.Windows;
using System.Windows.Controls;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Views;

public partial class EventLogWindow : Window
{
    private readonly SqliteEventStore? _eventStore;

    public EventLogWindow()
    {
        InitializeComponent();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FamilyGuard", "familyguard.db");

        if (File.Exists(dbPath))
        {
            _eventStore = new SqliteEventStore($"Data Source={dbPath};Mode=ReadOnly");
        }

        LoadEvents();
    }

    private void LoadEvents()
    {
        if (_eventStore is null)
            return;

        try
        {
            var filterItem = EventTypeFilter?.SelectedItem as ComboBoxItem;
            var filter = filterItem?.Content?.ToString() ?? "All Events";

            var events = filter switch
            {
                "Mic Auto-Muted" => _eventStore.QueryByEventType(EventType.MicAutoMuted),
                "Presence Changes" => _eventStore.QueryByEventType(EventType.PresenceStateChanged),
                "Settings Changes" => _eventStore.QueryByEventType(EventType.SettingsChanged),
                "Service Events" => _eventStore.QueryRecent(100),
                _ => _eventStore.QueryRecent(200)
            };

            EventGrid.ItemsSource = events.Select(e => new EventLogEntry(e)).ToList();
        }
        catch
        {
            // Database may be locked — silently ignore
        }
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        LoadEvents();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadEvents();
    }
}
