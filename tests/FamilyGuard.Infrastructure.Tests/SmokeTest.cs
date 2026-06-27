using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Platform.Abstractions;

namespace FamilyGuard.Infrastructure.Tests;

public class SmokeTest
{
    [Fact]
    public void StubPresenceDetector_ReturnsConfiguredValues()
    {
        var stub = new StubPresenceDetector
        {
            IdleTime = TimeSpan.FromSeconds(42),
            ControllerActive = true
        };

        stub.GetIdleTime().ShouldBe(TimeSpan.FromSeconds(42));
        stub.IsControllerActive().ShouldBeTrue();
    }
}
