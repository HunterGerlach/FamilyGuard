using Shouldly;
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

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void SessionId_InequalityByValue()
    {
        var a = new SessionId(3);
        var b = new SessionId(4);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void DeviceId_EqualityByValue()
    {
        var a = new DeviceId("mic-001");
        var b = new DeviceId("mic-001");

        a.ShouldBe(b);
    }

    [Fact]
    public void UserId_EqualityByValue()
    {
        var a = new UserId("S-1-5-21-123");
        var b = new UserId("S-1-5-21-123");

        a.ShouldBe(b);
    }

    [Fact]
    public void MicrophoneInfo_HoldsDeviceDetails()
    {
        var mic = new MicrophoneInfo(
            DeviceId: new DeviceId("mic-001"),
            Name: "HyperX QuadCast",
            IsCommunicationsDefault: true);

        mic.DeviceId.Value.ShouldBe("mic-001");
        mic.Name.ShouldBe("HyperX QuadCast");
        mic.IsCommunicationsDefault.ShouldBeTrue();
    }
}
