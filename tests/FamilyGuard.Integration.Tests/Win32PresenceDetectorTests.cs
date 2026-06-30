using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Platform.Windows;

namespace FamilyGuard.Integration.Tests;

[Trait("Category", "Windows")]
public class Win32PresenceDetectorTests
{
    [Fact]
    public void GetIdleTime_ReturnsNonNegative()
    {
        var detector = new Win32PresenceDetector();

        var idle = detector.GetIdleTime();

        idle.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void GetIdleTime_ReturnsReasonableValue()
    {
        var detector = new Win32PresenceDetector();

        var idle = detector.GetIdleTime();

        // Should be less than 50 days (GetTickCount rollover)
        idle.TotalDays.ShouldBeLessThan(50);
    }
}
