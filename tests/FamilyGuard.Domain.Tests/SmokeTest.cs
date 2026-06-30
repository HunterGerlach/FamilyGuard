using FluentAssertions;
using Xunit;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Tests;

public class SmokeTest
{
    [Fact]
    public void PresenceState_HasExpectedValues()
    {
        Enum.GetValues<PresenceState>().Should().HaveCount(4);
    }
}
