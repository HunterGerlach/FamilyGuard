using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Events;

public sealed record PolicyTriggeredEvent(
    string PolicyId,
    string PolicyName,
    PolicyActionType ActionType,
    DateTimeOffset OccurredAt) : IDomainEvent;
