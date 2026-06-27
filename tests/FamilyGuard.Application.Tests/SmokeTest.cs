using FluentAssertions;
using Xunit;
using FamilyGuard.Application.Ports.Input;

namespace FamilyGuard.Application.Tests;

public class SmokeTest
{
    [Fact]
    public void IPresenceDetector_InterfaceExists()
    {
        typeof(IPresenceDetector).IsInterface.Should().BeTrue();
    }
}
