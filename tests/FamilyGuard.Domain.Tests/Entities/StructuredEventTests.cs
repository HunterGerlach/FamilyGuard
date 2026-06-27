using FluentAssertions;
using Xunit;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Domain.Tests.Entities;

public class StructuredEventTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var evt = StructuredEvent.Create(
            eventType: EventType.MicAutoMuted,
            windowsUser: "child1",
            sessionId: new SessionId(3),
            policyId: "mute_unattended_microphone",
            timestamp: timestamp);

        evt.EventType.Should().Be(EventType.MicAutoMuted);
        evt.WindowsUser.Should().Be("child1");
        evt.SessionId.Value.Should().Be(3);
        evt.PolicyId.Should().Be("mute_unattended_microphone");
        evt.TimestampUtc.Should().Be(timestamp);
    }

    [Fact]
    public void Create_GeneratesId()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        evt.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithDetails_StoresAdditionalData()
    {
        var details = new Dictionary<string, string>
        {
            ["device_name"] = "HyperX QuadCast",
            ["inactive_seconds"] = "94"
        };

        var evt = StructuredEvent.Create(
            eventType: EventType.MicAutoMuted,
            windowsUser: "child1",
            sessionId: new SessionId(3),
            details: details);

        evt.Details.Should().ContainKey("device_name")
            .WhoseValue.Should().Be("HyperX QuadCast");
        evt.Details.Should().ContainKey("inactive_seconds")
            .WhoseValue.Should().Be("94");
    }

    [Fact]
    public void Create_WithoutDetails_HasEmptyDictionary()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        evt.Details.Should().BeEmpty();
    }

    [Fact]
    public void Events_AreImmutableAfterCreation()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        // Details dictionary should be read-only
        evt.Details.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
    }
}
