using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.UseCases;

public sealed class SuperviseSessionsUseCase
{
    private readonly ISessionMonitor _sessions;
    private readonly IAgentLifecycleManager _agents;

    public SuperviseSessionsUseCase(
        ISessionMonitor sessions,
        IAgentLifecycleManager agents)
    {
        _sessions = sessions;
        _agents = agents;
    }

    public void Execute()
    {
        var activeSessions = _sessions.GetActiveInteractiveSessions();
        var runningAgents = _agents.GetRunningAgentSessions();

        LaunchMissingAgents(activeSessions);
        StopOrphanedAgents(activeSessions, runningAgents);
    }

    public void ShutdownAll()
    {
        foreach (var session in _agents.GetRunningAgentSessions())
            _agents.StopAgent(session);
    }

    private void LaunchMissingAgents(IReadOnlyList<SessionId> activeSessions)
    {
        foreach (var session in activeSessions)
        {
            if (!_agents.IsAgentRunning(session))
                _agents.LaunchAgent(session);
        }
    }

    private void StopOrphanedAgents(
        IReadOnlyList<SessionId> activeSessions,
        IReadOnlyList<SessionId> runningAgents)
    {
        var activeSessionIds = activeSessions.Select(session => session.Value).ToHashSet();
        foreach (var agentSession in runningAgents)
        {
            if (!activeSessionIds.Contains(agentSession.Value))
                _agents.StopAgent(agentSession);
        }
    }
}
