using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace FamilyGuard.Infrastructure.Tests.Persistence;

public class PinRateLimiterTests
{
    private readonly FakeTimeProvider _clock = new();
    private readonly PinRateLimiter _limiter;

    public PinRateLimiterTests()
    {
        _limiter = new PinRateLimiter(_clock, maxAttempts: 5, lockoutMinutes: 15);
    }

    [Fact]
    public void IsLocked_FalseInitially()
    {
        _limiter.IsLocked.ShouldBeFalse();
    }

    [Fact]
    public void RecordFailure_UnderLimit_DoesNotLock()
    {
        for (int i = 0; i < 4; i++)
            _limiter.RecordFailure();

        _limiter.IsLocked.ShouldBeFalse();
    }

    [Fact]
    public void RecordFailure_AtLimit_Locks()
    {
        for (int i = 0; i < 5; i++)
            _limiter.RecordFailure();

        _limiter.IsLocked.ShouldBeTrue();
    }

    [Fact]
    public void Locked_ExpiresAfterLockoutPeriod()
    {
        for (int i = 0; i < 5; i++)
            _limiter.RecordFailure();

        _limiter.IsLocked.ShouldBeTrue();

        _clock.Advance(TimeSpan.FromMinutes(15));

        _limiter.IsLocked.ShouldBeFalse();
    }

    [Fact]
    public void RecordSuccess_ResetsCounter()
    {
        for (int i = 0; i < 4; i++)
            _limiter.RecordFailure();

        _limiter.RecordSuccess();
        _limiter.RemainingAttempts.ShouldBe(5);

        // One more failure shouldn't lock
        _limiter.RecordFailure();
        _limiter.IsLocked.ShouldBeFalse();
    }

    [Fact]
    public void RemainingAttempts_Decrements()
    {
        _limiter.RemainingAttempts.ShouldBe(5);
        _limiter.RecordFailure();
        _limiter.RemainingAttempts.ShouldBe(4);
    }

    [Fact]
    public void LockoutExpiresAt_SetWhenLocked()
    {
        for (int i = 0; i < 5; i++)
            _limiter.RecordFailure();

        _limiter.LockoutExpiresAt.ShouldNotBeNull();
    }
}
