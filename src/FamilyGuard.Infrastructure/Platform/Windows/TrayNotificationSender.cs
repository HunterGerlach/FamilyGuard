using FamilyGuard.Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Sends Windows toast/tray notifications. Phase 0 implementation logs only.
/// Phase 1 will use H.NotifyIcon or WinUI notification APIs.
/// </summary>
public sealed class TrayNotificationSender : INotificationSender
{
    private readonly ILogger<TrayNotificationSender> _logger;

    public TrayNotificationSender(ILogger<TrayNotificationSender> logger)
    {
        _logger = logger;
    }

    public void ShowNotification(string title, string message)
    {
        _logger.LogInformation("Notification: [{Title}] {Message}", title, message);
        // Phase 1: Replace with actual tray/toast notification
    }
}
