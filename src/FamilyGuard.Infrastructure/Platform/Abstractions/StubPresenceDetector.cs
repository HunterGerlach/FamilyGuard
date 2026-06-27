using FamilyGuard.Application.Ports.Input;

namespace FamilyGuard.Infrastructure.Platform.Abstractions;

public sealed class StubPresenceDetector : IPresenceDetector
{
    public TimeSpan IdleTime { get; set; }
    public bool ControllerActive { get; set; }

    public TimeSpan GetIdleTime() => IdleTime;
    public bool IsControllerActive() => ControllerActive;
}
