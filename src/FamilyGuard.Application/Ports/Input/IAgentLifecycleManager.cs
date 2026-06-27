using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Ports.Input;

public interface IAgentLifecycleManager
{
    void LaunchAgent(SessionId sessionId);
    void StopAgent(SessionId sessionId);
    bool IsAgentRunning(SessionId sessionId);
    IReadOnlyList<SessionId> GetRunningAgentSessions();
}
