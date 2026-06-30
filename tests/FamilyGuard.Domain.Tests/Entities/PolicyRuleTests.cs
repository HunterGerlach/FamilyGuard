using Shouldly;
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

        rule.Id.ShouldBe("mute_unattended_microphone");
        rule.Name.ShouldBe("Mute unattended microphone");
        rule.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Disabled_Rule_DoesNotEvaluate()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: false);

        rule.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Rule_HasConditions()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway]);

        rule.Conditions.Count.ShouldBe(2);
        rule.Conditions.ShouldContain(PolicyCondition.MicUnmuted);
        rule.Conditions.ShouldContain(PolicyCondition.PresenceAway);
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

        rule.Actions.Count.ShouldBe(1);
        rule.Actions[0].ActionType.ShouldBe(PolicyActionType.MuteMicrophone);
    }

    [Fact]
    public void Rule_WithAppliesToUsers_FiltersCorrectly()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            appliesToUsers: ["child1", "child2"]);

        rule.AppliesToUser("child1").ShouldBeTrue();
        rule.AppliesToUser("parent").ShouldBeFalse();
    }

    [Fact]
    public void Rule_WithNoUserFilter_AppliesToAll()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true);

        rule.AppliesToUser("anyone").ShouldBeTrue();
    }

    [Fact]
    public void Create_CopiesCollectionsToProtectInvariants()
    {
        var conditions = new List<PolicyCondition> { PolicyCondition.MicUnmuted };
        var actions = new List<PolicyAction> { new(PolicyActionType.MuteMicrophone) };

        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: conditions,
            actions: actions);

        conditions.Add(PolicyCondition.PresenceAway);
        actions.Add(new PolicyAction(PolicyActionType.LogEvent));

        rule.Conditions.ShouldBe([PolicyCondition.MicUnmuted]);
        rule.Actions.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankIdentity(string value)
    {
        Should.Throw<ArgumentException>(() => new PolicyRule(value, "Name", true));
        Should.Throw<ArgumentException>(() => new PolicyRule("id", value, true));
    }

    [Fact]
    public void Rule_NormalizesUserFilters()
    {
        var rule = new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            appliesToUsers: [" child1 ", "CHILD1"]);

        rule.AppliesToUsers.ShouldBe(["child1"]);
        rule.AppliesToUser("Child1").ShouldBeTrue();
    }

}
