using System.Reflection;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Service;

public sealed class ServiceWorker : BackgroundService
{
    private readonly SuperviseSessionsUseCase _superviseSessions;
    private readonly IEventStore _eventStore;
    private readonly IUpdateChecker? _updateChecker;
    private readonly ILogger<ServiceWorker> _logger;

    private DateTimeOffset _lastUpdateCheck = DateTimeOffset.MinValue;
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

    public ServiceWorker(
        SuperviseSessionsUseCase superviseSessions,
        IEventStore eventStore,
        ILogger<ServiceWorker> logger,
        IUpdateChecker? updateChecker = null)
    {
        _superviseSessions = superviseSessions ?? throw new ArgumentNullException(nameof(superviseSessions));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        _superviseSessions.Execute();
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
        _superviseSessions.ShutdownAll();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FamilyGuard.Service received stop signal");
        return base.StopAsync(cancellationToken);
    }
}
