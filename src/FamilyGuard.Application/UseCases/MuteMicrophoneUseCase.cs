using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.UseCases;

public sealed class MuteMicrophoneUseCase
{
    private readonly IMicrophoneController _mic;
    private readonly IEventStore _eventStore;
    private readonly INotificationSender _notifier;

    public MuteMicrophoneUseCase(
        IMicrophoneController mic,
        IEventStore eventStore,
        INotificationSender notifier)
    {
        _mic = mic;
        _eventStore = eventStore;
        _notifier = notifier;
    }

    public void Execute(string windowsUser, SessionId sessionId, string policyId)
    {
        var micInfo = _mic.GetDefaultCommunicationsMicrophone();
        if (micInfo is null)
            return;

        _mic.Mute();

        var details = new Dictionary<string, string>
        {
            ["device_id"] = micInfo.DeviceId.Value,
            ["device_name"] = micInfo.Name
        };

        _eventStore.Append(StructuredEvent.Create(
            eventType: EventType.MicAutoMuted,
            windowsUser: windowsUser,
            sessionId: sessionId,
            policyId: policyId,
            details: details));

        _notifier.ShowNotification(
            "DAD",
            "Microphone was muted because nobody appeared active at the computer.");
    }
}
