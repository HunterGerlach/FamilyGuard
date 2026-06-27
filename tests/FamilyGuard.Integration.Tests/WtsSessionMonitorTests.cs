using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using FamilyGuard.Infrastructure.Platform.Windows;

namespace FamilyGuard.Integration.Tests;

[Trait("Category", "Windows")]
public class WtsSessionMonitorTests
{
    [Fact]
    public void GetActiveInteractiveSessions_ReturnsAtLeastOne()
    {
        var monitor = new WtsSessionMonitor(NullLogger<WtsSessionMonitor>.Instance);

        var sessions = monitor.GetActiveInteractiveSessions();

        // The test runner is in an interactive session
        sessions.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetActiveInteractiveSessions_ExcludesSession0()
    {
        var monitor = new WtsSessionMonitor(NullLogger<WtsSessionMonitor>.Instance);

        var sessions = monitor.GetActiveInteractiveSessions();

        sessions.ShouldAllBe(s => s.Value > 0);
    }
}
