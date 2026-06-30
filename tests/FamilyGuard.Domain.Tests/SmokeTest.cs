using Shouldly;
using Xunit;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Tests;

public class SmokeTest
{
    [Fact]
    public void PresenceState_HasExpectedValues()
    {
        Enum.GetValues<PresenceState>().Length.ShouldBe(4);
    }
}
