namespace FamilyGuard.Application.Ports.Input;

/// <summary>
/// Monitors system-level events: sleep/wake, session lock/unlock,
/// device changes. Raises events for the agent to react to.
/// </summary>
public interface ISystemEventMonitor : IDisposable
{
    event Action? SessionLocked;
    event Action? SessionUnlocked;
    event Action? SystemSuspending;
    event Action? SystemResuming;
    event Action? DefaultMicrophoneChanged;

    void Start();
}
