using Shouldly;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace FamilyGuard.Application.Tests.UseCases;

/// <summary>
/// Tests the full agent run cycle: presence evaluation → policy evaluation → action execution.
/// This validates the complete flow without any Windows dependencies.
/// </summary>
public class AgentRunCycleTests
{
    private readonly IPresenceDetector _detector = Substitute.For<IPresenceDetector>();
    private readonly IMicrophoneController _mic = Substitute.For<IMicrophoneController>();
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly INotificationSender _notifier = Substitute.For<INotificationSender>();
    private readonly FakeTimeProvider _clock = new();

    private readonly EvaluatePresenceUseCase _evalPresence;
    private readonly EvaluatePolicyUseCase _evalPolicy;
    private readonly MuteMicrophoneUseCase _muteMic;
    private readonly PresenceStateMachine _stateMachine;
    private readonly PolicyEngine _policyEngine = new();

    private readonly PolicyRule[] _rules =
    [
        new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)])
    ];

    public AgentRunCycleTests()
    {
        _stateMachine = new PresenceStateMachine(90, _clock);
        _evalPresence = new EvaluatePresenceUseCase(_detector, _stateMachine, _clock);
        _evalPolicy = new EvaluatePolicyUseCase(_policyEngine, _mic);
        _muteMic = new MuteMicrophoneUseCase(_mic, _eventStore, _notifier);
    }

    private void RunCycle()
    {
        _evalPresence.Execute();
        var results = _evalPolicy.Execute(_rules, _stateMachine.CurrentState, "child1");
        foreach (var result in results)
        {
            foreach (var action in result.Actions)
            {
                if (action.ActionType == PolicyActionType.MuteMicrophone)
                    _muteMic.Execute("child1", new SessionId(1), result.Rule.Id);
            }
        }
    }

    [Fact]
    public void FullCycle_UserPresent_MicUnmuted_NoAction()
    {
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);
        _mic.IsMuted().Returns(false);

        RunCycle();

        _mic.DidNotReceive().Mute();
    }

    [Fact]
    public void FullCycle_UserAway_MicUnmuted_MutesMic()
    {
        // Establish presence first
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);
        _mic.IsMuted().Returns(false);
        _mic.GetDefaultCommunicationsMicrophone()
            .Returns(new MicrophoneInfo(new DeviceId("mic-1"), "Test Mic", true));
        RunCycle();

        // Go idle past threshold
        _clock.Advance(TimeSpan.FromSeconds(91));
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(91));

        RunCycle();

        _mic.Received(1).Mute();
        _eventStore.Received(1).Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.MicAutoMuted));
        _notifier.Received(1).ShowNotification(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void FullCycle_UserAway_MicAlreadyMuted_NoAction()
    {
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);
        _mic.IsMuted().Returns(true);
        RunCycle();

        _clock.Advance(TimeSpan.FromSeconds(91));
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(91));

        RunCycle();

        _mic.DidNotReceive().Mute();
    }

    [Fact]
    public void FullCycle_ControllerActive_NeverMutes()
    {
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(100));
        _detector.IsControllerActive().Returns(true);
        _mic.IsMuted().Returns(false);

        // Simulate 10 minutes of controller gaming
        for (int i = 0; i < 60; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(10));
            RunCycle();
        }

        _mic.DidNotReceive().Mute();
    }

    [Fact]
    public void FullCycle_UserReturns_DoesNotAutoUnmute()
    {
        // Setup: user goes away, mic gets muted
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);
        _mic.IsMuted().Returns(false);
        _mic.GetDefaultCommunicationsMicrophone()
            .Returns(new MicrophoneInfo(new DeviceId("mic-1"), "Test Mic", true));
        RunCycle();

        _clock.Advance(TimeSpan.FromSeconds(91));
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(91));
        RunCycle();
        _mic.Received(1).Mute();

        // User returns — mic stays muted (IsMuted now true)
        _mic.ClearReceivedCalls();
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _mic.IsMuted().Returns(true);
        RunCycle();

        // Verify: no unmute call exists anywhere in the system
        _stateMachine.CurrentState.ShouldBe(PresenceState.Present);
        // IMicrophoneController has no Unmute() method by design
    }

    [Fact]
    public void FullCycle_LikelyAway_DoesNotMute()
    {
        _detector.GetIdleTime().Returns(TimeSpan.Zero);
        _detector.IsControllerActive().Returns(false);
        _mic.IsMuted().Returns(false);
        RunCycle();

        // 75% of threshold = likely_away, not away
        _clock.Advance(TimeSpan.FromSeconds(68));
        _detector.GetIdleTime().Returns(TimeSpan.FromSeconds(68));
        RunCycle();

        _stateMachine.CurrentState.ShouldBe(PresenceState.LikelyAway);
        _mic.DidNotReceive().Mute();
    }
}
