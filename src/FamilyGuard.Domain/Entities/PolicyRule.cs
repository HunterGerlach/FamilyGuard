using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Entities;

public sealed class PolicyRule
{
    public string Id { get; }
    public string Name { get; }
    public bool Enabled { get; }
    public IReadOnlyList<PolicyCondition> Conditions { get; }
    public IReadOnlyList<PolicyAction> Actions { get; }

    public IReadOnlyList<string>? AppliesToUsers { get; }

    private readonly HashSet<string>? _appliesToUsersSet;

    public PolicyRule(
        string id,
        string name,
        bool enabled,
        IReadOnlyList<PolicyCondition>? conditions = null,
        IReadOnlyList<PolicyAction>? actions = null,
        IReadOnlyList<string>? appliesToUsers = null)
    {
        Id = id;
        Name = name;
        Enabled = enabled;
        Conditions = conditions ?? [];
        Actions = actions ?? [];
        AppliesToUsers = appliesToUsers;
        _appliesToUsersSet = appliesToUsers is { Count: > 0 }
            ? new HashSet<string>(appliesToUsers, StringComparer.OrdinalIgnoreCase)
            : null;
    }

    public bool AppliesToUser(string windowsUser)
    {
        return _appliesToUsersSet is null || _appliesToUsersSet.Contains(windowsUser);
    }
}
