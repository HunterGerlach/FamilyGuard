using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;

namespace FamilyGuard.Application.UseCases;

public sealed class SuperviseSessionsUseCase
{
    private readonly ISessionMonitor _sessions;
    private readonly IAgentLifecycleManager _agents;
    private readonly IEventStore _events;

    public SuperviseSessionsUseCase(
        ISessionMonitor sessions,
        IAgentLifecycleManager agents,
        IEventStore events)
    {
        _sessions = sessions;
        _agents = agents;
        _events = events;
    }

    public void Execute()
    {
        var activeSessions = _sessions.GetActiveInteractiveSessions();
        var runningAgents = _agents.GetRunningAgentSessions();

        // Launch agents for new sessions
        foreach (var session in activeSessions)
        {
            if (!_agents.IsAgentRunning(session))
                _agents.LaunchAgent(session);
        }

        // Stop agents for departed sessions
        var activeSet = new HashSet<int>(activeSessions.Select(s => s.Value));
        foreach (var agentSession in runningAgents)
        {
            if (!activeSet.Contains(agentSession.Value))
                _agents.StopAgent(agentSession);
        }
    }

    public void ShutdownAll()
    {
        foreach (var session in _agents.GetRunningAgentSessions())
            _agents.StopAgent(session);
    }
}
