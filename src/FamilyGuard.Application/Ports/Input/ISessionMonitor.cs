using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Ports.Input;

public interface ISessionMonitor
{
    SessionState GetSessionState(SessionId sessionId);
    IReadOnlyList<SessionId> GetActiveInteractiveSessions();
}
