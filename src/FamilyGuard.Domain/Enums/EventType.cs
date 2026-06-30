namespace FamilyGuard.Domain.Enums;

public enum EventType
{
    AgentStarted,
    AgentStopped,
    ServiceStarted,
    ServiceStopped,
    PresenceStateChanged,
    MicAutoMuted,
    MicManualMuted,
    MicManualUnmuted,
    SettingsUnlockSuccess,
    SettingsUnlockFailed,
    SettingsChanged,
    PolicyEnabled,
    PolicyDisabled,
    DeviceChanged,
    ControllerDetectionFailed,
    UpdateCheckStarted,
    UpdateInstalled,
    UpdateFailed,
    MigrationStarted,
    MigrationCompleted,
    PolicyError
}
