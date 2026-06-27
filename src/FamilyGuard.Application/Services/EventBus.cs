using System.Collections.Concurrent;
using FamilyGuard.Domain.Events;

namespace FamilyGuard.Application.Services;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : IDomainEvent
    {
        var handlers = _handlers.GetOrAdd(typeof(T), _ => []);
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public void Publish<T>(T domainEvent) where T : IDomainEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var handlers))
            return;

        Delegate[] snapshot;
        lock (handlers)
        {
            snapshot = [.. handlers];
        }

        foreach (var handler in snapshot)
        {
            ((Action<T>)handler)(domainEvent);
        }
    }
}
