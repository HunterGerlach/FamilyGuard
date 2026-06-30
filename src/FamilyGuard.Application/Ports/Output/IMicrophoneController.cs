using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Ports.Output;

public interface IMicrophoneController
{
    MicrophoneInfo? GetDefaultCommunicationsMicrophone();
    bool IsMuted();
    void Mute();
    void Unmute();
}
