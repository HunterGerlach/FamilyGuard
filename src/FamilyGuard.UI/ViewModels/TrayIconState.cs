namespace FamilyGuard.UI.ViewModels;

public enum TrayIconState
{
    /// <summary>Green — monitoring active, presence normal.</summary>
    Normal,

    /// <summary>Yellow — mic open and presence weakening (likely_away).</summary>
    Warning,

    /// <summary>Orange/Red — mic was auto-muted by policy.</summary>
    ActionTaken,

    /// <summary>Gray — service disconnected or monitoring problem.</summary>
    Disconnected
}
