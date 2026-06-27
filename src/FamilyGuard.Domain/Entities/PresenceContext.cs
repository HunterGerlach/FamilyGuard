using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Entities;

public sealed class PresenceContext
{
    public PresenceState State { get; private set; } = PresenceState.Unknown;
    public DateTimeOffset LastStateChangeAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public double InactiveSeconds { get; private set; }
    public bool SessionLocked { get; set; }
    public bool MicUnmuted { get; set; }

    public void UpdateState(PresenceState newState, DateTimeOffset timestamp)
    {
        if (newState == State)
            return;

        State = newState;
        LastStateChangeAt = timestamp;
    }

    public void RecordActivity(DateTimeOffset timestamp)
    {
        LastActivityAt = timestamp;
    }

    public double GetInactiveSeconds(DateTimeOffset now)
    {
        if (LastActivityAt == default)
            return 0;

        return (now - LastActivityAt).TotalSeconds;
    }
}
