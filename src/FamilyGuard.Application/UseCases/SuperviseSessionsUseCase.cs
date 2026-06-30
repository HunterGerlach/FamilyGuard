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
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    }

    public void Execute()
    {
        var activeSessions = _sessions.GetActiveInteractiveSessions();
        var runningAgents = _agents.GetRunningAgentSessions();
        var activeSessionValues = activeSessions.Select(session => session.Value).ToHashSet();

        LaunchMissingAgents(activeSessions);
        StopAgentsWithoutActiveSession(runningAgents, activeSessionValues);
    }

    public void ShutdownAll()
    {
        foreach (var session in _agents.GetRunningAgentSessions())
        {
            _agents.StopAgent(session);
        }
    }

    private void LaunchMissingAgents(IReadOnlyList<SessionId> activeSessions)
    {
        foreach (var session in activeSessions)
        {
            if (!_agents.IsAgentRunning(session))
            {
                _agents.LaunchAgent(session);
            }
        }
    }

    private void StopAgentsWithoutActiveSession(
        IReadOnlyList<SessionId> runningAgents,
        HashSet<int> activeSessionValues)
    {
        foreach (var agentSession in runningAgents)
        {
            if (!activeSessionValues.Contains(agentSession.Value))
            {
                _agents.StopAgent(agentSession);
            }
        }
    }
}
