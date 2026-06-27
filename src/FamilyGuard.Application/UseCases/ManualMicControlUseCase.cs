using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.UseCases;

public sealed class ManualMicControlUseCase
{
    private readonly IMicrophoneController _mic;
    private readonly IEventStore _eventStore;

    public ManualMicControlUseCase(IMicrophoneController mic, IEventStore eventStore)
    {
        _mic = mic;
        _eventStore = eventStore;
    }

    public void MuteManually(string windowsUser, SessionId sessionId)
    {
        _mic.Mute();
        _eventStore.Append(StructuredEvent.Create(
            EventType.MicManualMuted, windowsUser, sessionId));
    }

    public void UnmuteManually(string windowsUser, SessionId sessionId)
    {
        _mic.Unmute();
        _eventStore.Append(StructuredEvent.Create(
            EventType.MicManualUnmuted, windowsUser, sessionId));
    }
}
