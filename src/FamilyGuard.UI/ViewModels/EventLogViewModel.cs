using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.UI.ViewModels;

public partial class EventLogViewModel : ObservableObject
{
    private readonly IEventStore _eventStore;

    public ObservableCollection<EventLogEntry> Events { get; } = [];

    [ObservableProperty]
    private string _selectedFilter = "All Events";

    public EventLogViewModel(IEventStore eventStore)
    {
        _eventStore = eventStore;
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        Events.Clear();

        var events = SelectedFilter switch
        {
            "Mic Auto-Muted" => _eventStore.QueryByEventType(EventType.MicAutoMuted),
            "Presence Changes" => _eventStore.QueryByEventType(EventType.PresenceStateChanged),
            "Settings Changes" => _eventStore.QueryByEventType(EventType.SettingsChanged),
            "Service Events" => _eventStore.QueryRecent(100),
            _ => _eventStore.QueryRecent(200)
        };

        foreach (var evt in events)
        {
            Events.Add(new EventLogEntry(evt));
        }
    }
}

public sealed class EventLogEntry
{
    public DateTimeOffset TimestampUtc { get; }
    public string EventType { get; }
    public string WindowsUser { get; }
    public string? PolicyId { get; }
    public string DetailsDisplay { get; }

    public EventLogEntry(StructuredEvent evt)
    {
        TimestampUtc = evt.TimestampUtc;
        EventType = evt.EventType.ToString();
        WindowsUser = evt.WindowsUser;
        PolicyId = evt.PolicyId;
        DetailsDisplay = evt.Details.Count > 0
            ? string.Join(", ", evt.Details.Select(kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;
    }
}
