using FamilyGuard.Domain.Entities;

namespace FamilyGuard.Application.Policies;

public sealed record PolicyEvaluationResult(
    PolicyRule Rule,
    IReadOnlyList<PolicyAction> Actions);
