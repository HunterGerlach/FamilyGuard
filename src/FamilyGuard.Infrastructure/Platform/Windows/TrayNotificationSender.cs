using System.Runtime.Versioning;
using FamilyGuard.Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Sends notifications. Raises an event for the UI layer (FamilyGuard.UI)
/// to display via H.NotifyIcon balloon. Falls back to logging if no
/// subscriber is attached.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class TrayNotificationSender : INotificationSender
{
    private readonly ILogger<TrayNotificationSender> _logger;

    /// <summary>
    /// Raised when a notification should be shown. The UI layer subscribes
    /// and displays the actual balloon/toast via H.NotifyIcon.
    /// </summary>
    public event Action<string, string>? NotificationRequested;

    public TrayNotificationSender(ILogger<TrayNotificationSender> logger)
    {
        _logger = logger;
    }

    public void ShowNotification(string title, string message)
    {
        _logger.LogInformation("Notification: [{Title}] {Message}", title, message);
        NotificationRequested?.Invoke(title, message);
    }
}
