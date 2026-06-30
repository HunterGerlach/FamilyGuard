using Shouldly;
using Xunit;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.Infrastructure.Tests.Persistence;

public class SqliteEventStoreTests : IDisposable
{
    private readonly SqliteEventStore _store;

    public SqliteEventStoreTests()
    {
        _store = new SqliteEventStore("Data Source=:memory:");
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    [Fact]
    public void Append_ThenQueryRecent_ReturnsEvent()
    {
        var evt = StructuredEvent.Create(
            eventType: EventType.AgentStarted,
            windowsUser: "child1",
            sessionId: new SessionId(1));

        _store.Append(evt);
        var results = _store.QueryRecent();

        results.Count.ShouldBe(1);
        results[0].EventType.ShouldBe(EventType.AgentStarted);
        results[0].WindowsUser.ShouldBe("child1");
    }

    [Fact]
    public void Append_MultipleThenQueryByType_FiltersCorrectly()
    {
        _store.Append(StructuredEvent.Create(EventType.AgentStarted, "child1", new SessionId(1)));
        _store.Append(StructuredEvent.Create(EventType.MicAutoMuted, "child1", new SessionId(1)));
        _store.Append(StructuredEvent.Create(EventType.AgentStopped, "child1", new SessionId(1)));

        var results = _store.QueryByEventType(EventType.MicAutoMuted);

        results.Count.ShouldBe(1);
        results[0].EventType.ShouldBe(EventType.MicAutoMuted);
    }

    [Fact]
    public void Append_ThenQueryByTimeRange_FiltersCorrectly()
    {
        var t1 = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 6, 30, 11, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        _store.Append(StructuredEvent.Create(EventType.AgentStarted, "child1", new SessionId(1), timestamp: t1));
        _store.Append(StructuredEvent.Create(EventType.MicAutoMuted, "child1", new SessionId(1), timestamp: t2));
        _store.Append(StructuredEvent.Create(EventType.AgentStopped, "child1", new SessionId(1), timestamp: t3));

        var results = _store.QueryByTimeRange(
            t1.AddMinutes(30),
            t2.AddMinutes(30));

        results.Count.ShouldBe(1);
        results[0].EventType.ShouldBe(EventType.MicAutoMuted);
    }

    [Fact]
    public void Append_WithDetails_PreservesDetails()
    {
        var details = new Dictionary<string, string>
        {
            ["device_name"] = "HyperX QuadCast",
            ["inactive_seconds"] = "94"
        };

        var evt = StructuredEvent.Create(
            EventType.MicAutoMuted, "child1", new SessionId(3), details: details);

        _store.Append(evt);
        var results = _store.QueryRecent();

        results[0].Details["device_name"].ShouldBe("HyperX QuadCast");
    }

    [Fact]
    public void QueryRecent_RespectsLimit()
    {
        for (int i = 0; i < 10; i++)
        {
            _store.Append(StructuredEvent.Create(EventType.AgentStarted, "child1", new SessionId(1)));
        }

        var results = _store.QueryRecent(limit: 3);

        results.Count.ShouldBe(3);
    }

    [Fact]
    public void QueryRecent_ReturnsMostRecentFirst()
    {
        var t1 = new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 6, 30, 11, 0, 0, TimeSpan.Zero);

        _store.Append(StructuredEvent.Create(EventType.AgentStarted, "child1", new SessionId(1), timestamp: t1));
        _store.Append(StructuredEvent.Create(EventType.MicAutoMuted, "child1", new SessionId(1), timestamp: t2));

        var results = _store.QueryRecent();

        results[0].TimestampUtc.ShouldBe(t2);
    }
}
