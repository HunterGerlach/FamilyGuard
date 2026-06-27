namespace FamilyGuard.Domain.ValueObjects;

public sealed record MicrophoneInfo(
    DeviceId DeviceId,
    string Name,
    bool IsCommunicationsDefault);
