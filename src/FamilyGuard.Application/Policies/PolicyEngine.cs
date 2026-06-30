using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.Policies;

public sealed class PolicyEngine : IPolicyEngine
{
    public IReadOnlyList<PolicyEvaluationResult> Evaluate(
        IReadOnlyList<PolicyRule> rules,
        PolicyEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<PolicyEvaluationResult>();

        foreach (var rule in rules)
        {
            if (!rule.Enabled)
                continue;

            if (!rule.AppliesToUser(context.WindowsUser))
                continue;

            if (!AllConditionsMet(rule.Conditions, context))
                continue;

            results.Add(new PolicyEvaluationResult(rule, rule.Actions));
        }

        return results;
    }

    private static bool AllConditionsMet(
        IReadOnlyList<PolicyCondition> conditions,
        PolicyEvaluationContext context)
    {
        foreach (var condition in conditions)
        {
            if (!IsConditionMet(condition, context))
                return false;
        }
        return true;
    }

    private static bool IsConditionMet(PolicyCondition condition, PolicyEvaluationContext context)
    {
        return condition switch
        {
            PolicyCondition.MicUnmuted => context.MicUnmuted,
            PolicyCondition.PresenceAway => context.PresenceState == PresenceState.Away,
            PolicyCondition.PresenceLikelyAway => context.PresenceState == PresenceState.LikelyAway,
            PolicyCondition.SessionLocked => context.SessionLocked,
            _ => false
        };
    }
}
