using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.UI.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    [ObservableProperty]
    private TrayIconState _iconState = TrayIconState.Disconnected;

    [ObservableProperty]
    private string _toolTipText = "DaD — Connecting...";

    [ObservableProperty]
    private PresenceState _presenceState = PresenceState.Unknown;

    [ObservableProperty]
    private bool _micMuted = true;

    [ObservableProperty]
    private string _micDeviceName = "Unknown";

    [ObservableProperty]
    private double _inactiveSeconds;

    [ObservableProperty]
    private string _lastAction = "None";

    public ICommand MuteNowCommand { get; }
    public ICommand UnmuteCommand { get; }

    public TrayViewModel()
    {
        MuteNowCommand = new RelayCommand(OnMuteNow);
        UnmuteCommand = new RelayCommand(OnUnmute);
    }

    public void UpdateState(TrayIconState state)
    {
        IconState = state;
        ToolTipText = state switch
        {
            TrayIconState.Normal => "DaD — Monitoring active",
            TrayIconState.Warning => "DaD — Mic open, user may be away",
            TrayIconState.ActionTaken => "DaD — Mic was auto-muted",
            TrayIconState.Disconnected => "DaD — Service disconnected",
            _ => "DaD"
        };
    }

    /// <summary>
    /// Called by the agent to update UI state each cycle.
    /// Determines the correct tray icon state based on presence + mic.
    /// </summary>
    public void UpdateFromAgentState(
        PresenceState presence,
        bool micMuted,
        double inactiveSeconds,
        string? micName = null)
    {
        PresenceState = presence;
        MicMuted = micMuted;
        InactiveSeconds = inactiveSeconds;
        if (micName is not null) MicDeviceName = micName;

        // Determine tray state per spec:
        // Yellow = mic open AND presence weakening (likely_away)
        // Not merely "mic is unmuted" during normal use
        if (!micMuted && presence == PresenceState.LikelyAway)
            UpdateState(TrayIconState.Warning);
        else if (!micMuted && presence == PresenceState.Away)
            UpdateState(TrayIconState.ActionTaken);
        else
            UpdateState(TrayIconState.Normal);
    }

    public void NotifyMicAutoMuted()
    {
        LastAction = $"Mic auto-muted at {DateTime.Now:HH:mm:ss}";
        UpdateState(TrayIconState.ActionTaken);
    }

    public void NotifyServiceDisconnected()
    {
        UpdateState(TrayIconState.Disconnected);
    }

    private void OnMuteNow()
    {
        // Will be wired to IMicrophoneController via DI
        LastAction = $"Manual mute at {DateTime.Now:HH:mm:ss}";
    }

    private void OnUnmute()
    {
        // Will be wired to IMicrophoneController via DI
        LastAction = $"Manual unmute at {DateTime.Now:HH:mm:ss}";
    }
}
