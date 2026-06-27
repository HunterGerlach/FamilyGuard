using FluentAssertions;
using Xunit;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Domain.Tests.ValueObjects;

public class ValueObjectTests
{
    [Fact]
    public void SessionId_EqualityByValue()
    {
        var a = new SessionId(3);
        var b = new SessionId(3);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void SessionId_InequalityByValue()
    {
        var a = new SessionId(3);
        var b = new SessionId(4);

        a.Should().NotBe(b);
    }

    [Fact]
    public void DeviceId_EqualityByValue()
    {
        var a = new DeviceId("mic-001");
        var b = new DeviceId("mic-001");

        a.Should().Be(b);
    }

    [Fact]
    public void UserId_EqualityByValue()
    {
        var a = new UserId("S-1-5-21-123");
        var b = new UserId("S-1-5-21-123");

        a.Should().Be(b);
    }

    [Fact]
    public void MicrophoneInfo_HoldsDeviceDetails()
    {
        var mic = new MicrophoneInfo(
            DeviceId: new DeviceId("mic-001"),
            Name: "HyperX QuadCast",
            IsCommunicationsDefault: true);

        mic.DeviceId.Value.Should().Be("mic-001");
        mic.Name.Should().Be("HyperX QuadCast");
        mic.IsCommunicationsDefault.Should().BeTrue();
    }
}
