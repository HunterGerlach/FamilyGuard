using FamilyGuard.Domain.Entities;

namespace FamilyGuard.Application.Policies;

public interface IPolicyEngine
{
    IReadOnlyList<PolicyEvaluationResult> Evaluate(
        IReadOnlyList<PolicyRule> rules,
        PolicyEvaluationContext context);
}
