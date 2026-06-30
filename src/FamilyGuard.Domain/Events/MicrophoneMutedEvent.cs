using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Domain.Events;

public sealed record MicrophoneMutedEvent(
    DeviceId DeviceId,
    string DeviceName,
    string PolicyId,
    string WindowsUser,
    DateTimeOffset OccurredAt) : IDomainEvent;
