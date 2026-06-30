using FamilyGuard.Application.Ports.Output;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Sends Windows tray balloon notifications via NotifyIcon.
/// Falls back to logging if no tray icon is available.
/// </summary>
public sealed class TrayNotificationSender : INotificationSender
{
    private readonly ILogger<TrayNotificationSender> _logger;
    private NotifyIcon? _notifyIcon;

    public TrayNotificationSender(ILogger<TrayNotificationSender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Set by the UI layer when the tray icon is initialized.
    /// </summary>
    public void SetNotifyIcon(NotifyIcon icon)
    {
        _notifyIcon = icon;
    }

    public void ShowNotification(string title, string message)
    {
        _logger.LogInformation("Notification: [{Title}] {Message}", title, message);

        if (_notifyIcon is not null)
        {
            try
            {
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                _notifyIcon.ShowBalloonTip(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to show balloon notification, falling back to log");
            }
        }
    }
}
