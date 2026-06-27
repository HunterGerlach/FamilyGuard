namespace FamilyGuard.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
