using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.Ports.Output;

public interface IEventStore
{
    void Append(StructuredEvent structuredEvent);
    IReadOnlyList<StructuredEvent> QueryByTimeRange(DateTimeOffset from, DateTimeOffset to);
    IReadOnlyList<StructuredEvent> QueryByEventType(EventType eventType, int limit = 100);
    IReadOnlyList<StructuredEvent> QueryRecent(int limit = 50);
}
