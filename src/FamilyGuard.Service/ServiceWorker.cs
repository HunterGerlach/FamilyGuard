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
    private readonly ISettingsRepository _settings;
    private readonly IUpdateChecker? _updateChecker;
    private readonly ApplyUpdateUseCase? _applyUpdate;
    private readonly IUpdateInstaller? _updateInstaller;
    private readonly ILogger<ServiceWorker> _logger;

    private DateTimeOffset _lastUpdateCheck = DateTimeOffset.MinValue;
    private TimeSpan _jitterOffset = TimeSpan.Zero;
    private bool _jitterInitialized;

    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan MaxJitter = TimeSpan.FromMinutes(30);

    public ServiceWorker(
        SuperviseSessionsUseCase superviseSessions,
        IEventStore eventStore,
        ISettingsRepository settings,
        ILogger<ServiceWorker> logger,
        IUpdateChecker? updateChecker = null,
        ApplyUpdateUseCase? applyUpdate = null,
        IUpdateInstaller? updateInstaller = null)
    {
        _superviseSessions = superviseSessions ?? throw new ArgumentNullException(nameof(superviseSessions));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _updateChecker = updateChecker;
        _applyUpdate = applyUpdate;
        _updateInstaller = updateInstaller;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FamilyGuard.Service starting");

        _eventStore.Append(StructuredEvent.Create(
            EventType.ServiceStarted, "SYSTEM", new SessionId(0)));

        // Clean up any temp files from previous failed updates
        _updateInstaller?.CleanupTempFiles();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _superviseSessions.Execute();
                await CheckForUpdatesIfDue(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in service supervision loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _superviseSessions.ShutdownAll();

        _eventStore.Append(StructuredEvent.Create(
            EventType.ServiceStopped, "SYSTEM", new SessionId(0)));

        _logger.LogInformation("FamilyGuard.Service stopping");
    }

    private async Task CheckForUpdatesIfDue(CancellationToken ct)
    {
        if (_updateChecker is null)
            return;

        // Initialize jitter once (random 0-30 min offset to avoid thundering herd)
        if (!_jitterInitialized)
        {
            _jitterOffset = TimeSpan.FromMinutes(Random.Shared.Next(0, (int)MaxJitter.TotalMinutes));
            _jitterInitialized = true;
        }

        var effectiveInterval = UpdateCheckInterval + _jitterOffset;
        if (DateTimeOffset.UtcNow - _lastUpdateCheck < effectiveInterval)
            return;

        _lastUpdateCheck = DateTimeOffset.UtcNow;

        _eventStore.Append(StructuredEvent.Create(
            EventType.UpdateCheckStarted, "SYSTEM", new SessionId(0)));

        var currentVersion = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "0.0.0";

        var update = await _updateChecker.CheckForUpdateAsync(currentVersion, ct);
        if (update is null)
            return;

        _logger.LogInformation("Update available: {Version} (current: {Current})",
            update.Version, currentVersion);

        // Route based on auto-update setting (Content-Based Router pattern)
        var settings = _settings.Load();
        if (!settings.AutoUpdateEnabled || _applyUpdate is null)
        {
            // Log-only mode: notify but don't act
            _eventStore.Append(StructuredEvent.Create(
                EventType.UpdateAvailable, "SYSTEM", new SessionId(0),
                details: new Dictionary<string, string>
                {
                    ["version"] = update.Version,
                    ["current_version"] = currentVersion,
                    ["auto_update"] = "disabled"
                }));
            return;
        }

        // Auto-update mode: download, verify, apply
        _logger.LogInformation("Auto-update enabled — applying update {Version}", update.Version);
        await _applyUpdate.ExecuteAsync(update, ct);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FamilyGuard.Service received stop signal");
        return base.StopAsync(cancellationToken);
    }
}
