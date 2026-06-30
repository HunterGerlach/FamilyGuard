using FluentAssertions;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.Services;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.Events;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Tests.UseCases;

public class MuteMicrophoneUseCaseTests
{
    private readonly IMicrophoneController _mic = Substitute.For<IMicrophoneController>();
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly INotificationSender _notifier = Substitute.For<INotificationSender>();
    private readonly MuteMicrophoneUseCase _useCase;

    public MuteMicrophoneUseCaseTests()
    {
        _useCase = new MuteMicrophoneUseCase(_mic, _eventStore, _notifier);
    }

    [Fact]
    public void Execute_MutesMicrophone()
    {
        _mic.GetDefaultCommunicationsMicrophone()
            .Returns(new MicrophoneInfo(new DeviceId("mic-1"), "Test Mic", true));

        _useCase.Execute("child1", new SessionId(3), "mute_unattended_microphone");

        _mic.Received(1).Mute();
    }

    [Fact]
    public void Execute_LogsEvent()
    {
        _mic.GetDefaultCommunicationsMicrophone()
            .Returns(new MicrophoneInfo(new DeviceId("mic-1"), "Test Mic", true));

        _useCase.Execute("child1", new SessionId(3), "mute_unattended_microphone");

        _eventStore.Received(1).Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.MicAutoMuted &&
            e.WindowsUser == "child1" &&
            e.PolicyId == "mute_unattended_microphone"));
    }

    [Fact]
    public void Execute_SendsNotification()
    {
        _mic.GetDefaultCommunicationsMicrophone()
            .Returns(new MicrophoneInfo(new DeviceId("mic-1"), "Test Mic", true));

        _useCase.Execute("child1", new SessionId(3), "mute_unattended_microphone");

        _notifier.Received(1).ShowNotification(
            Arg.Any<string>(),
            Arg.Is<string>(msg => msg.Contains("muted")));
    }

    [Fact]
    public void Execute_NoMicDetected_DoesNotMute()
    {
        _mic.GetDefaultCommunicationsMicrophone().Returns((MicrophoneInfo?)null);

        _useCase.Execute("child1", new SessionId(3), "mute_unattended_microphone");

        _mic.DidNotReceive().Mute();
    }
}
