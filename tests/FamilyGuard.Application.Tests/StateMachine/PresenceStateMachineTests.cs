using FluentAssertions;
using Xunit;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.Events;
using Microsoft.Extensions.Time.Testing;

namespace FamilyGuard.Application.Tests.StateMachine;

public class PresenceStateMachineTests
{
    private readonly FakeTimeProvider _clock = new();
    private readonly List<IDomainEvent> _publishedEvents = [];

    private PresenceStateMachine CreateMachine(int timeoutSeconds = 90)
    {
        var machine = new PresenceStateMachine(timeoutSeconds, _clock);
        machine.OnStateChanged += evt => _publishedEvents.Add(evt);
        return machine;
    }

    [Fact]
    public void InitialState_IsUnknown()
    {
        var machine = CreateMachine();

        machine.CurrentState.Should().Be(PresenceState.Unknown);
    }

    [Fact]
    public void ActivityDetected_TransitionsToPresent()
    {
        var machine = CreateMachine();

        machine.RecordActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void ActivityDetected_FromUnknown_RaisesEvent()
    {
        var machine = CreateMachine();

        machine.RecordActivity();

        _publishedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PresenceChangedEvent>()
            .Which.NewState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void NoActivity_AfterThreshold75Percent_TransitionsToLikelyAway()
    {
        var machine = CreateMachine(timeoutSeconds: 100);
        machine.RecordActivity();
        _publishedEvents.Clear();

        _clock.Advance(TimeSpan.FromSeconds(75));
        machine.Evaluate();

        machine.CurrentState.Should().Be(PresenceState.LikelyAway);
    }

    [Fact]
    public void NoActivity_AfterFullThreshold_TransitionsToAway()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();
        _publishedEvents.Clear();

        _clock.Advance(TimeSpan.FromSeconds(90));
        machine.Evaluate();

        machine.CurrentState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void Activity_DuringLikelyAway_TransitionsBackToPresent()
    {
        var machine = CreateMachine(timeoutSeconds: 100);
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(76));
        machine.Evaluate();
        machine.CurrentState.Should().Be(PresenceState.LikelyAway);

        _publishedEvents.Clear();
        machine.RecordActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
        _publishedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PresenceChangedEvent>()
            .Which.PreviousState.Should().Be(PresenceState.LikelyAway);
    }

    [Fact]
    public void Activity_DuringAway_TransitionsBackToPresent()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(91));
        machine.Evaluate();
        machine.CurrentState.Should().Be(PresenceState.Away);

        _publishedEvents.Clear();
        machine.RecordActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void SessionLocked_TransitionsToAway()
    {
        var machine = CreateMachine();
        machine.RecordActivity();
        _publishedEvents.Clear();

        machine.SetSessionLocked(true);

        machine.CurrentState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void SessionUnlocked_WithActivity_TransitionsToPresent()
    {
        var machine = CreateMachine();
        machine.RecordActivity();
        machine.SetSessionLocked(true);
        _publishedEvents.Clear();

        machine.SetSessionLocked(false);
        machine.RecordActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void SessionDisconnected_TransitionsToAway()
    {
        var machine = CreateMachine();
        machine.RecordActivity();
        _publishedEvents.Clear();

        machine.SetSessionDisconnected(true);

        machine.CurrentState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void LikelyAway_TransitionsToAway_AfterFullThreshold()
    {
        var machine = CreateMachine(timeoutSeconds: 100);
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(75));
        machine.Evaluate();
        machine.CurrentState.Should().Be(PresenceState.LikelyAway);

        _clock.Advance(TimeSpan.FromSeconds(25));
        machine.Evaluate();
        machine.CurrentState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void RapidActivity_DuringPresent_StaysPresent()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(10));
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(10));
        machine.RecordActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void Evaluate_BeforeAnyActivity_StaysUnknown()
    {
        var machine = CreateMachine();

        machine.Evaluate();

        machine.CurrentState.Should().Be(PresenceState.Unknown);
    }

    [Fact]
    public void InactiveSeconds_TracksElapsedTime()
    {
        var machine = CreateMachine();
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(42));

        machine.InactiveSeconds.Should().BeApproximately(42, 1);
    }

    [Fact]
    public void ControllerActivity_ResetsIdleTimer()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();

        _clock.Advance(TimeSpan.FromSeconds(80));
        machine.RecordControllerActivity();

        machine.CurrentState.Should().Be(PresenceState.Present);
        machine.InactiveSeconds.Should().BeApproximately(0, 1);
    }

    [Fact]
    public void ControllerActivity_PreventsTransitionToAway()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();

        // Simulate 10 minutes of controller-only gaming
        for (int i = 0; i < 60; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(10));
            machine.RecordControllerActivity();
            machine.Evaluate();
        }

        machine.CurrentState.Should().Be(PresenceState.Present);
    }

    [Fact]
    public void StateChange_RaisesEventWithPreviousAndNewState()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();
        _publishedEvents.Clear();

        _clock.Advance(TimeSpan.FromSeconds(90));
        machine.Evaluate();

        var evt = _publishedEvents.OfType<PresenceChangedEvent>().Last();
        evt.PreviousState.Should().Be(PresenceState.Present);
        evt.NewState.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void SameState_Evaluate_DoesNotRaiseEvent()
    {
        var machine = CreateMachine(timeoutSeconds: 90);
        machine.RecordActivity();
        _publishedEvents.Clear();

        _clock.Advance(TimeSpan.FromSeconds(10));
        machine.Evaluate();

        _publishedEvents.Should().BeEmpty();
    }
}
