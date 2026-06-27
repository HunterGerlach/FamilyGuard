using Shouldly;
using Xunit;
using FamilyGuard.Application.Policies;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.Tests.Policies;

public class PolicyEngineTests
{
    [Fact]
    public void Evaluate_MatchingRule_ReturnsActions()
    {
        var engine = new PolicyEngine();
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)]);

        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Away,
            MicUnmuted = true,
            WindowsUser = "child1"
        };

        var results = engine.Evaluate([rule], state);

        results.Count.ShouldBe(1);
        results[0].Rule.Id.ShouldBe("mute_unattended_microphone");
        var action = results[0].Actions.ShouldHaveSingleItem();
        action.ActionType.ShouldBe(PolicyActionType.MuteMicrophone);
    }

    [Fact]
    public void Evaluate_NonMatchingCondition_ReturnsEmpty()
    {
        var engine = new PolicyEngine();
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)]);

        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Present,
            MicUnmuted = true,
            WindowsUser = "child1"
        };

        var results = engine.Evaluate([rule], state);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_DisabledRule_Skipped()
    {
        var engine = new PolicyEngine();
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: false,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)]);

        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Away,
            MicUnmuted = true,
            WindowsUser = "child1"
        };

        var results = engine.Evaluate([rule], state);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_UserNotInAppliesTo_Skipped()
    {
        var engine = new PolicyEngine();
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)],
            appliesToUsers: ["child1"]);

        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Away,
            MicUnmuted = true,
            WindowsUser = "parent"
        };

        var results = engine.Evaluate([rule], state);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_MultipleRules_OnlyMatchingFire()
    {
        var engine = new PolicyEngine();
        var rules = new[]
        {
            new PolicyRule("rule1", "Rule 1", true,
                conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
                actions: [new PolicyAction(PolicyActionType.MuteMicrophone)]),
            new PolicyRule("rule2", "Rule 2", true,
                conditions: [PolicyCondition.SessionLocked],
                actions: [new PolicyAction(PolicyActionType.LogEvent)])
        };

        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Away,
            MicUnmuted = true,
            SessionLocked = false,
            WindowsUser = "child1"
        };

        var results = engine.Evaluate(rules, state);

        results.Count.ShouldBe(1);
        results[0].Rule.Id.ShouldBe("rule1");
    }

    [Fact]
    public void Evaluate_EmptyRules_ReturnsEmpty()
    {
        var engine = new PolicyEngine();
        var state = new PolicyEvaluationContext
        {
            PresenceState = PresenceState.Away,
            MicUnmuted = true,
            WindowsUser = "child1"
        };

        var results = engine.Evaluate([], state);

        results.ShouldBeEmpty();
    }
}
