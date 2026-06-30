namespace FamilyGuard.Application.Ports.Input;

public interface IPresenceDetector
{
    TimeSpan GetIdleTime();
    bool IsControllerActive();
}
