using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Service;

/// <summary>
/// The main service worker. Supervises per-user FamilyGuard.Agent processes
/// for each active interactive session.
/// </summary>
public sealed class ServiceWorker : BackgroundService
{
    private readonly ISessionMonitor _sessions;
    private readonly IAgentLifecycleManager _agents;
    private readonly IEventStore _eventStore;
    private readonly ILogger<ServiceWorker> _logger;

    public ServiceWorker(
        ISessionMonitor sessions,
        IAgentLifecycleManager agents,
        IEventStore eventStore,
        ILogger<ServiceWorker> logger)
    {
        _sessions = sessions;
        _agents = agents;
        _eventStore = eventStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FamilyGuard.Service starting");

        _eventStore.Append(StructuredEvent.Create(
            EventType.ServiceStarted, "SYSTEM", new SessionId(0)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                SuperviseSessions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in service supervision loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        ShutdownAllAgents();

        _eventStore.Append(StructuredEvent.Create(
            EventType.ServiceStopped, "SYSTEM", new SessionId(0)));

        _logger.LogInformation("FamilyGuard.Service stopping");
    }

    private void SuperviseSessions()
    {
        var activeSessions = _sessions.GetActiveInteractiveSessions();
        var runningAgents = _agents.GetRunningAgentSessions();

        // Launch agents for new sessions
        foreach (var session in activeSessions)
        {
            if (!_agents.IsAgentRunning(session))
            {
                _logger.LogInformation("Launching agent for session {SessionId}", session.Value);
                _agents.LaunchAgent(session);
            }
        }

        // Stop agents for sessions that are no longer active
        var activeSet = new HashSet<int>(activeSessions.Select(s => s.Value));
        foreach (var agentSession in runningAgents)
        {
            if (!activeSet.Contains(agentSession.Value))
            {
                _logger.LogInformation("Stopping agent for departed session {SessionId}", agentSession.Value);
                _agents.StopAgent(agentSession);
            }
        }
    }

    private void ShutdownAllAgents()
    {
        foreach (var session in _agents.GetRunningAgentSessions())
        {
            _logger.LogInformation("Stopping agent for session {SessionId} during shutdown", session.Value);
            _agents.StopAgent(session);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FamilyGuard.Service received stop signal");
        return base.StopAsync(cancellationToken);
    }
}
