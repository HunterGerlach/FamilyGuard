using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Entities;

public sealed record PolicyAction(
    PolicyActionType ActionType,
    string? Message = null);
