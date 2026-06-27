using FamilyGuard.Domain.Entities;

namespace FamilyGuard.Application.Ports.Output;

public interface IPolicyRepository
{
    IReadOnlyList<PolicyRule> LoadAll();
    void Save(PolicyRule rule);
    void Delete(string ruleId);
    void EnsureDefaultRules();
}
