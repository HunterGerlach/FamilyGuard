using FluentAssertions;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.Tests.UseCases;

public class EvaluatePolicyUseCaseTests
{
    private readonly IPolicyEngine _engine = Substitute.For<IPolicyEngine>();
    private readonly IMicrophoneController _mic = Substitute.For<IMicrophoneController>();
    private readonly EvaluatePolicyUseCase _useCase;

    public EvaluatePolicyUseCaseTests()
    {
        _useCase = new EvaluatePolicyUseCase(_engine, _mic);
    }

    [Fact]
    public void Execute_WithMatchingRules_ReturnsResults()
    {
        var rules = new[]
        {
            new PolicyRule("mute_mic", "Mute mic", true,
                conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
                actions: [new PolicyAction(PolicyActionType.MuteMicrophone)])
        };

        _mic.IsMuted().Returns(false);

        _engine.Evaluate(Arg.Any<IReadOnlyList<PolicyRule>>(), Arg.Any<PolicyEvaluationContext>())
            .Returns(new[]
            {
                new PolicyEvaluationResult(rules[0], rules[0].Actions)
            });

        var results = _useCase.Execute(rules, PresenceState.Away, "child1");

        results.Should().ContainSingle();
    }

    [Fact]
    public void Execute_BuildsContextFromCurrentState()
    {
        var rules = Array.Empty<PolicyRule>();
        _mic.IsMuted().Returns(true);

        _engine.Evaluate(Arg.Any<IReadOnlyList<PolicyRule>>(), Arg.Any<PolicyEvaluationContext>())
            .Returns([]);

        _useCase.Execute(rules, PresenceState.Present, "child1");

        _engine.Received(1).Evaluate(
            Arg.Any<IReadOnlyList<PolicyRule>>(),
            Arg.Is<PolicyEvaluationContext>(ctx =>
                ctx.PresenceState == PresenceState.Present &&
                ctx.MicUnmuted == false &&
                ctx.WindowsUser == "child1"));
    }
}
