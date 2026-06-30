using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.Policies;

public sealed class PolicyEvaluationContext
{
    public required PresenceState PresenceState { get; init; }
    public required bool MicUnmuted { get; init; }
    public bool SessionLocked { get; init; }
    public required string WindowsUser { get; init; }
}
