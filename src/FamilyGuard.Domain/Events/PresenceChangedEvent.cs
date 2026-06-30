using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Events;

public sealed record PresenceChangedEvent(
    PresenceState PreviousState,
    PresenceState NewState,
    DateTimeOffset OccurredAt) : IDomainEvent;
