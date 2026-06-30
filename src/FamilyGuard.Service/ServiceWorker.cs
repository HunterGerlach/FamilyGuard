using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Service;

/// <summary>
/// The main service worker. Supervises per-user FamilyGuard.Agent processes
/// for each active interactive session.
/// </summary>
public sealed class ServiceWorker : BackgroundService
{
    private readonly ILogger<ServiceWorker> _logger;

    public ServiceWorker(ILogger<ServiceWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FamilyGuard.Service starting");

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

        _logger.LogInformation("FamilyGuard.Service stopping");
    }

    private void SuperviseSessions()
    {
        // Phase 0: Log supervision tick. Full WTS integration in Phase 1.
        _logger.LogDebug("Supervision tick");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FamilyGuard.Service received stop signal");
        return base.StopAsync(cancellationToken);
    }
}
