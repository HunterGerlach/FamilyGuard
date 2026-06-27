using Shouldly;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Tests.UseCases;

public class ManualMicControlUseCaseTests
{
    private readonly IMicrophoneController _mic = Substitute.For<IMicrophoneController>();
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly ManualMicControlUseCase _useCase;

    public ManualMicControlUseCaseTests()
    {
        _useCase = new ManualMicControlUseCase(_mic, _eventStore);
    }

    [Fact]
    public void MuteManually_CallsMute()
    {
        _useCase.MuteManually("child1", new SessionId(1));

        _mic.Received(1).Mute();
    }

    [Fact]
    public void MuteManually_LogsEvent()
    {
        _useCase.MuteManually("child1", new SessionId(1));

        _eventStore.Received(1).Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.MicManualMuted &&
            e.WindowsUser == "child1"));
    }

    [Fact]
    public void UnmuteManually_CallsUnmute()
    {
        _useCase.UnmuteManually("child1", new SessionId(1));

        _mic.Received(1).Unmute();
    }

    [Fact]
    public void UnmuteManually_LogsEvent()
    {
        _useCase.UnmuteManually("child1", new SessionId(1));

        _eventStore.Received(1).Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.MicManualUnmuted &&
            e.WindowsUser == "child1"));
    }
}
