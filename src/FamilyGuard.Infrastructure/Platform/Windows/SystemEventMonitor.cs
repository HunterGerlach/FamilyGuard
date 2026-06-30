using FamilyGuard.Application.Ports.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Monitors Windows system events: session lock/unlock, sleep/wake.
/// Uses Microsoft.Win32.SystemEvents for session and power events.
/// </summary>
public sealed class SystemEventMonitor : ISystemEventMonitor
{
    private readonly ILogger<SystemEventMonitor> _logger;
    private bool _started;

    public event Action? SessionLocked;
    public event Action? SessionUnlocked;
    public event Action? SystemSuspending;
    public event Action? SystemResuming;
    public event Action? DefaultMicrophoneChanged;

    public SystemEventMonitor(ILogger<SystemEventMonitor> logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _logger.LogInformation("System event monitoring started");
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        _logger.LogDebug("Session switch event: {Reason}", e.Reason);

        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
                SessionLocked?.Invoke();
                break;
            case SessionSwitchReason.SessionUnlock:
                SessionUnlocked?.Invoke();
                break;
            case SessionSwitchReason.SessionLogoff:
            case SessionSwitchReason.RemoteDisconnect:
                SessionLocked?.Invoke();
                break;
            case SessionSwitchReason.SessionLogon:
            case SessionSwitchReason.RemoteConnect:
                SessionUnlocked?.Invoke();
                break;
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        _logger.LogDebug("Power mode changed: {Mode}", e.Mode);

        switch (e.Mode)
        {
            case PowerModes.Suspend:
                SystemSuspending?.Invoke();
                break;
            case PowerModes.Resume:
                SystemResuming?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Call when the default audio endpoint changes. Will be wired to
    /// IMMNotificationClient.OnDefaultDeviceChanged in a future iteration.
    /// </summary>
    public void RaiseDefaultMicrophoneChanged()
    {
        _logger.LogInformation("Default microphone changed");
        DefaultMicrophoneChanged?.Invoke();
    }

    public void Dispose()
    {
        if (!_started) return;

        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        _logger.LogInformation("System event monitoring stopped");
    }
}
