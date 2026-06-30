using FamilyGuard.Domain.Events;

namespace FamilyGuard.Application.Services;

public interface IEventBus
{
    void Publish<T>(T domainEvent) where T : IDomainEvent;
    void Subscribe<T>(Action<T> handler) where T : IDomainEvent;
}
