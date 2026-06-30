using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Platform.Windows;

namespace FamilyGuard.Integration.Tests;

[Trait("Category", "Windows")]
public class XInputControllerDetectorTests
{
    [Fact]
    public void IsAnyControllerConnected_DoesNotThrow()
    {
        var detector = new XInputControllerDetector();

        // May return true or false depending on hardware — just verify no crash
        Should.NotThrow(() => detector.IsAnyControllerConnected());
    }

    [Fact]
    public void HasNewInput_DoesNotThrow()
    {
        var detector = new XInputControllerDetector();

        Should.NotThrow(() => detector.HasNewInput());
    }
}
