using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Domain.Entities;

public sealed class StructuredEvent
{
    public Guid Id { get; private init; }
    public DateTimeOffset TimestampUtc { get; private init; }
    public EventType EventType { get; private init; }
    public string WindowsUser { get; private init; } = string.Empty;
    public SessionId SessionId { get; private init; }
    public string? PolicyId { get; private init; }
    public IReadOnlyDictionary<string, string> Details { get; private init; } = new Dictionary<string, string>();

    private StructuredEvent() { }

    public static StructuredEvent Create(
        EventType eventType,
        string windowsUser,
        SessionId sessionId,
        string? policyId = null,
        DateTimeOffset? timestamp = null,
        Dictionary<string, string>? details = null)
    {
        return new StructuredEvent
        {
            Id = Guid.NewGuid(),
            TimestampUtc = timestamp ?? DateTimeOffset.UtcNow,
            EventType = eventType,
            WindowsUser = windowsUser,
            SessionId = sessionId,
            PolicyId = policyId,
            Details = details is not null
                ? new Dictionary<string, string>(details)
                : new Dictionary<string, string>()
        };
    }
}
