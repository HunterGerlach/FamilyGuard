namespace FamilyGuard.Application.Ports.Output;

public interface INotificationSender
{
    void ShowNotification(string title, string message);
}
