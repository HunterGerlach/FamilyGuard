namespace FamilyGuard.Infrastructure.Persistence;

public sealed class PinRateLimiter
{
    private readonly TimeProvider _clock;
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;

    private int _failureCount;
    private DateTimeOffset? _lockoutExpiresAt;

    public bool IsLocked
    {
        get
        {
            if (_lockoutExpiresAt is null)
                return false;

            if (_clock.GetUtcNow() >= _lockoutExpiresAt)
            {
                _lockoutExpiresAt = null;
                _failureCount = 0;
                return false;
            }

            return true;
        }
    }

    public int RemainingAttempts => IsLocked ? 0 : _maxAttempts - _failureCount;
    public DateTimeOffset? LockoutExpiresAt => _lockoutExpiresAt;

    public PinRateLimiter(TimeProvider clock, int maxAttempts = 5, int lockoutMinutes = 15)
    {
        _clock = clock;
        _maxAttempts = maxAttempts;
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);
    }

    public void RecordFailure()
    {
        _failureCount++;

        if (_failureCount >= _maxAttempts)
        {
            _lockoutExpiresAt = _clock.GetUtcNow() + _lockoutDuration;
        }
    }

    public void RecordSuccess()
    {
        _failureCount = 0;
        _lockoutExpiresAt = null;
    }
}
