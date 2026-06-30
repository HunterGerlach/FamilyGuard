using FamilyGuard.Domain;
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
        Id = Guard.NotBlank(id, nameof(id));
        Name = Guard.NotBlank(name, nameof(name));
        Enabled = enabled;
        Conditions = Guard.Snapshot(conditions);
        Actions = Guard.Snapshot(actions);
        AppliesToUsers = NormalizeUsers(appliesToUsers);
        _appliesToUsersSet = AppliesToUsers is { Count: > 0 }
            ? new HashSet<string>(AppliesToUsers, StringComparer.OrdinalIgnoreCase)
            : null;
    }

    public bool AppliesToUser(string windowsUser)
    {
        Guard.NotBlank(windowsUser, nameof(windowsUser));
        return _appliesToUsersSet is null || _appliesToUsersSet.Contains(windowsUser);
    }

    private static IReadOnlyList<string>? NormalizeUsers(IReadOnlyList<string>? users)
    {
        if (users is null or { Count: 0 })
            return null;

        return users
            .Select(user => Guard.NotBlank(user, nameof(users)).Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
