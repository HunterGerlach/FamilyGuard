namespace FamilyGuard.Application.Ports.Output;

public interface ISettingsRepository
{
    ProtectedSettings Load();
    void Save(ProtectedSettings settings);
    void SetPin(string pin);
    bool VerifyPin(string pin);
    void SaveAgentState(AgentState state);
    AgentState? LoadAgentState();
}

public sealed class AgentState
{
    public string PresenceState { get; set; } = "Unknown";
    public bool MicMuted { get; set; } = true;
    public string MicDeviceName { get; set; } = "Unknown";
    public double InactiveSeconds { get; set; }
    public string LastAction { get; set; } = "None";
    public string LastUpdatedUtc { get; set; } = string.Empty;
}

public sealed class ProtectedSettings
{
    public int PresenceTimeoutSeconds { get; set; } = 90;
    public List<string> CoveredUsers { get; set; } = [];
    public string PinHash { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public bool AutoUpdateEnabled { get; set; } = true;
    public string UpdateChannelUrl { get; set; } = UpdateDefaults.StableChannelUrl;
}
