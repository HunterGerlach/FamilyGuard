using System.Reflection;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Service;

public sealed class ServiceWorker : BackgroundService
{
    private readonly ISessionMonitor _sessions;
    private readonly IAgentLifecycleManager _agents;
    private readonly IEventStore _eventStore;
    private readonly IUpdateChecker? _updateChecker;
    private readonly ILogger<ServiceWorker> _logger;

    private DateTimeOffset _lastUpdateCheck = DateTimeOffset.MinValue;
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

    public ServiceWorker(
        ISessionMonitor sessions,
        IAgentLifecycleManager agents,
        IEventStore eventStore,
        ILogger<ServiceWorker> logger,
        IUpdateChecker? updateChecker = null)
    {
        _sessions = sessions;
        _agents = agents;
        _eventStore = eventStore;
        _logger = logger;
        _updateChecker = updateChecker;
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
                await CheckForUpdatesIfDue(stoppingToken);
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

        foreach (var session in activeSessions)
        {
            if (!_agents.IsAgentRunning(session))
            {
                _logger.LogInformation("Launching agent for session {SessionId}", session.Value);
                _agents.LaunchAgent(session);
            }
        }

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

    private async Task CheckForUpdatesIfDue(CancellationToken ct)
    {
        if (_updateChecker is null)
            return;

        if (DateTimeOffset.UtcNow - _lastUpdateCheck < UpdateCheckInterval)
            return;

        _lastUpdateCheck = DateTimeOffset.UtcNow;

        _eventStore.Append(StructuredEvent.Create(
            EventType.UpdateCheckStarted, "SYSTEM", new SessionId(0)));

        var currentVersion = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "0.0.0";

        var update = await _updateChecker.CheckForUpdateAsync(currentVersion, ct);

        if (update is not null)
        {
            _logger.LogInformation(
                "Update available: {Version} (current: {Current})",
                update.Version, currentVersion);

            _eventStore.Append(StructuredEvent.Create(
                EventType.UpdateCheckStarted, "SYSTEM", new SessionId(0),
                details: new Dictionary<string, string>
                {
                    ["available_version"] = update.Version,
                    ["current_version"] = currentVersion,
                    ["download_url"] = update.DownloadUrl
                }));
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
