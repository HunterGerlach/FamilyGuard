using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.Events;

namespace FamilyGuard.Application.StateMachine;

public sealed class PresenceStateMachine
{
    private readonly int _timeoutSeconds;
    private readonly TimeProvider _clock;

    private DateTimeOffset _lastActivityAt;
    private bool _hasActivity;
    private bool _sessionLocked;
    private bool _sessionDisconnected;

    public PresenceState CurrentState { get; private set; } = PresenceState.Unknown;
    public event Action<PresenceChangedEvent>? OnStateChanged;

    public double InactiveSeconds =>
        _hasActivity ? (_clock.GetUtcNow() - _lastActivityAt).TotalSeconds : 0;

    public PresenceStateMachine(int timeoutSeconds, TimeProvider clock)
    {
        _timeoutSeconds = timeoutSeconds;
        _clock = clock;
    }

    public void RecordActivity()
    {
        _lastActivityAt = _clock.GetUtcNow();
        _hasActivity = true;
        TransitionTo(PresenceState.Present);
    }

    public void RecordControllerActivity()
    {
        RecordActivity();
    }

    public void SetSessionLocked(bool locked)
    {
        _sessionLocked = locked;
        if (locked)
        {
            TransitionTo(PresenceState.Away);
        }
    }

    public void SetSessionDisconnected(bool disconnected)
    {
        _sessionDisconnected = disconnected;
        if (disconnected)
        {
            TransitionTo(PresenceState.Away);
        }
    }

    public void Evaluate()
    {
        if (_sessionLocked || _sessionDisconnected)
        {
            TransitionTo(PresenceState.Away);
            return;
        }

        if (!_hasActivity)
        {
            return;
        }

        var inactive = (_clock.GetUtcNow() - _lastActivityAt).TotalSeconds;

        if (inactive >= _timeoutSeconds)
        {
            TransitionTo(PresenceState.Away);
        }
        else if (inactive >= _timeoutSeconds * 0.75)
        {
            TransitionTo(PresenceState.LikelyAway);
        }
        else
        {
            TransitionTo(PresenceState.Present);
        }
    }

    private void TransitionTo(PresenceState newState)
    {
        if (newState == CurrentState)
            return;

        var previous = CurrentState;
        CurrentState = newState;

        OnStateChanged?.Invoke(new PresenceChangedEvent(
            PreviousState: previous,
            NewState: newState,
            OccurredAt: _clock.GetUtcNow()));
    }
}
