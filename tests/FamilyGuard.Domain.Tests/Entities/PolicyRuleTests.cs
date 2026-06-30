using FluentAssertions;
using Xunit;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Tests.Entities;

public class PolicyRuleTests
{
    [Fact]
    public void Create_SetsIdAndName()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true);

        rule.Id.Should().Be("mute_unattended_microphone");
        rule.Name.Should().Be("Mute unattended microphone");
        rule.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Disabled_Rule_DoesNotEvaluate()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: false);

        rule.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Rule_HasConditions()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway]);

        rule.Conditions.Should().HaveCount(2);
        rule.Conditions.Should().Contain(PolicyCondition.MicUnmuted);
        rule.Conditions.Should().Contain(PolicyCondition.PresenceAway);
    }

    [Fact]
    public void Rule_HasActions()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)]);

        rule.Actions.Should().HaveCount(1);
        rule.Actions[0].ActionType.Should().Be(PolicyActionType.MuteMicrophone);
    }

    [Fact]
    public void Rule_WithAppliesToUsers_FiltersCorrectly()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            appliesToUsers: ["child1", "child2"]);

        rule.AppliesToUser("child1").Should().BeTrue();
        rule.AppliesToUser("parent").Should().BeFalse();
    }

    [Fact]
    public void Rule_WithNoUserFilter_AppliesToAll()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true);

        rule.AppliesToUser("anyone").Should().BeTrue();
    }
}
