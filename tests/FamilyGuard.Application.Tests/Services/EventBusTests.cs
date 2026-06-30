using FluentAssertions;
using Xunit;
using FamilyGuard.Application.Services;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.Events;

namespace FamilyGuard.Application.Tests.Services;

public class EventBusTests
{
    [Fact]
    public void Publish_CallsSubscribedHandler()
    {
        var bus = new EventBus();
        IDomainEvent? received = null;
        bus.Subscribe<PresenceChangedEvent>(evt => received = evt);

        var domainEvent = new PresenceChangedEvent(
            PresenceState.Unknown, PresenceState.Present, DateTimeOffset.UtcNow);
        bus.Publish(domainEvent);

        received.Should().Be(domainEvent);
    }

    [Fact]
    public void Publish_CallsMultipleSubscribers()
    {
        var bus = new EventBus();
        var calls = new List<IDomainEvent>();
        bus.Subscribe<PresenceChangedEvent>(evt => calls.Add(evt));
        bus.Subscribe<PresenceChangedEvent>(evt => calls.Add(evt));

        var domainEvent = new PresenceChangedEvent(
            PresenceState.Unknown, PresenceState.Present, DateTimeOffset.UtcNow);
        bus.Publish(domainEvent);

        calls.Should().HaveCount(2);
    }

    [Fact]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();
        var domainEvent = new PresenceChangedEvent(
            PresenceState.Unknown, PresenceState.Present, DateTimeOffset.UtcNow);

        var act = () => bus.Publish(domainEvent);

        act.Should().NotThrow();
    }

    [Fact]
    public void Publish_OnlyCallsMatchingTypeSubscribers()
    {
        var bus = new EventBus();
        var presenceCalls = new List<IDomainEvent>();
        var micCalls = new List<IDomainEvent>();

        bus.Subscribe<PresenceChangedEvent>(evt => presenceCalls.Add(evt));
        bus.Subscribe<MicrophoneMutedEvent>(evt => micCalls.Add(evt));

        var domainEvent = new PresenceChangedEvent(
            PresenceState.Unknown, PresenceState.Present, DateTimeOffset.UtcNow);
        bus.Publish(domainEvent);

        presenceCalls.Should().HaveCount(1);
        micCalls.Should().BeEmpty();
    }
}
