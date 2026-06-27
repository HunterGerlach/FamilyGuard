using CommunityToolkit.Mvvm.ComponentModel;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.UI.ViewModels;

public partial class StatusViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentUser = Environment.UserName;

    [ObservableProperty]
    private string _monitoringStatus = "Active";

    [ObservableProperty]
    private string _micDeviceName = "Unknown";

    [ObservableProperty]
    private string _micState = "Unknown";

    [ObservableProperty]
    private string _presenceState = "Unknown";

    [ObservableProperty]
    private double _inactiveSeconds;

    [ObservableProperty]
    private string _lastPolicyAction = "None";

    public void Update(
        PresenceState presence,
        bool micMuted,
        string micName,
        double inactiveSeconds,
        string? lastAction)
    {
        MicDeviceName = micName;
        MicState = micMuted ? "Muted" : "Unmuted";
        PresenceState = presence.ToString();
        InactiveSeconds = inactiveSeconds;
        if (lastAction is not null)
            LastPolicyAction = lastAction;
    }
}
