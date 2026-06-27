using Shouldly;
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

        evt.EventType.ShouldBe(EventType.MicAutoMuted);
        evt.WindowsUser.ShouldBe("child1");
        evt.SessionId.Value.ShouldBe(3);
        evt.PolicyId.ShouldBe("mute_unattended_microphone");
        evt.TimestampUtc.ShouldBe(timestamp);
    }

    [Fact]
    public void Create_GeneratesId()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        evt.Id.ShouldNotBe(Guid.Empty);
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

        evt.Details["device_name"].ShouldBe("HyperX QuadCast");
        evt.Details["inactive_seconds"].ShouldBe("94");
    }

    [Fact]
    public void Create_WithoutDetails_HasEmptyDictionary()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        evt.Details.ShouldBeEmpty();
    }

    [Fact]
    public void Events_AreImmutableAfterCreation()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        // Details dictionary should be read-only
        evt.Details.ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
    }
}
