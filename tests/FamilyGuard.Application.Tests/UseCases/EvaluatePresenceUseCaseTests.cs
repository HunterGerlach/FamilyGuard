using FluentAssertions;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.Events;
using Microsoft.Extensions.Time.Testing;

namespace FamilyGuard.Application.Tests.UseCases;

public class EvaluatePresenceUseCaseTests
{
    private readonly IPresenceDetector _detector = Substitute.For<IPresenceDetector>();
    private readonly FakeTimeProvider _clock = new();
    private readonly PresenceStateMachine _stateMachine;
    private readonly EvaluatePresenceUseCase _useCase;
    private readonly List<PresenceChangedEvent> _events = [];

    public EvaluatePresenceUseCaseTests()
    {
        _stateMachine = new PresenceStateMachine(90, _clock);
        _stateMachine.OnStateChanged += evt => _events.Add(evt);
        _useCase = new EvaluatePresenceUseCase(_detector, _stateMachine, _clock);
    }

    [Fact]
    public void Execute_WithRecentActivity_TransitionsToPresent()
    {
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);

        _useCase.Execute();

        _stateMachine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void Execute_WithLongIdle_TransitionsToAway()
    {
        // First establish presence
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(0));
        _detector.IsControllerActive().Returns(false);
        _useCase.Execute();

        // Then go idle
        _clock.Advance(TimeSpan.FromSeconds(91));
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(91));
        _useCase.Execute();

        _stateMachine.CurrentState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void Execute_WithControllerActive_StaysPresent()
    {
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(100));
        _detector.IsControllerActive().Returns(true);

        _useCase.Execute();

        _stateMachine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void Execute_TransitionRaisesEvent()
    {
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(0));
        _detector.IsControllerActive().Returns(false);

        _useCase.Execute();

        _events.Should().ContainSingle()
            .Which.NewState.Should().Be(PresenceState.Present);
    }
}
