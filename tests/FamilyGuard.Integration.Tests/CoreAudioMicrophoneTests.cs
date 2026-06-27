using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using FamilyGuard.Infrastructure.Platform.Windows;

namespace FamilyGuard.Integration.Tests;

[Trait("Category", "Windows")]
public class CoreAudioMicrophoneTests
{
    [Fact]
    public void GetDefaultCommunicationsMicrophone_DoesNotThrow()
    {
        using var controller = new CoreAudioMicrophoneController(
            NullLogger<CoreAudioMicrophoneController>.Instance);

        // May return null if no mic — just verify no crash
        Should.NotThrow(() => controller.GetDefaultCommunicationsMicrophone());
    }

    [Fact]
    public void IsMuted_DoesNotThrow()
    {
        using var controller = new CoreAudioMicrophoneController(
            NullLogger<CoreAudioMicrophoneController>.Instance);

        Should.NotThrow(() => controller.IsMuted());
    }

    [SkippableFact]
    public void GetDefaultMic_WhenAvailable_ReturnsInfo()
    {
        using var controller = new CoreAudioMicrophoneController(
            NullLogger<CoreAudioMicrophoneController>.Instance);

        var mic = controller.GetDefaultCommunicationsMicrophone();
        Skip.If(mic is null, "No default communications microphone available");

        mic.Name.ShouldNotBeNullOrWhiteSpace();
        mic.DeviceId.Value.ShouldNotBeNullOrWhiteSpace();
        mic.IsCommunicationsDefault.ShouldBeTrue();
    }
}
