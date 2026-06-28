using Shouldly;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Tests.UseCases;

public class ApplyUpdateUseCaseTests
{
    private readonly IUpdateInstaller _installer = Substitute.For<IUpdateInstaller>();
    private readonly IHashVerifier _hashVerifier = Substitute.For<IHashVerifier>();
    private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
    private readonly SuperviseSessionsUseCase _supervisor;
    private readonly ApplyUpdateUseCase _useCase;

    private readonly UpdateInfo _update = new(
        Version: "1.0.0",
        Sha256: "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        DownloadUrl: "https://example.com/FamilyGuard.msi",
        ReleasedAt: DateTimeOffset.UtcNow);

    public ApplyUpdateUseCaseTests()
    {
        var sessions = Substitute.For<ISessionMonitor>();
        var agents = Substitute.For<IAgentLifecycleManager>();
        _supervisor = new SuperviseSessionsUseCase(sessions, agents);
        _useCase = new ApplyUpdateUseCase(_installer, _hashVerifier, _eventStore, _supervisor);
    }

    [Fact]
    public async Task HappyPath_DownloadVerifyShutdownApply()
    {
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(new UpdateDownloadResult(true, FilePath: "/tmp/FamilyGuard.msi"));

        _hashVerifier.VerifyAsync("/tmp/FamilyGuard.msi", _update.Sha256, Arg.Any<CancellationToken>())
            .Returns(true);

        _installer.ApplyAsync("/tmp/FamilyGuard.msi", Arg.Any<CancellationToken>())
            .Returns(new UpdateApplyResult(true, ExitCode: 0));

        var result = await _useCase.ExecuteAsync(_update);

        result.ShouldBeTrue();

        // Verify all steps were called
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e => e.EventType == EventType.UpdateAvailable));
        await _installer.Received().DownloadAsync(_update, Arg.Any<CancellationToken>());
        await _hashVerifier.Received().VerifyAsync("/tmp/FamilyGuard.msi", _update.Sha256, Arg.Any<CancellationToken>());
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e => e.EventType == EventType.UpdateDownloaded));
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e => e.EventType == EventType.UpdateApplying));
        await _installer.Received().ApplyAsync("/tmp/FamilyGuard.msi", Arg.Any<CancellationToken>());
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e => e.EventType == EventType.UpdateInstalled));
    }

    [Fact]
    public async Task DownloadFails_LogsFailure_NoInstallAttempted()
    {
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(new UpdateDownloadResult(false, Error: "Network timeout after 3 retries"));

        var result = await _useCase.ExecuteAsync(_update);

        result.ShouldBeFalse();
        await _hashVerifier.DidNotReceive().VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _installer.DidNotReceive().ApplyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.UpdateFailed));
    }

    [Fact]
    public async Task HashMismatch_LogsFailure_CleansUp_NoInstall()
    {
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(new UpdateDownloadResult(true, FilePath: "/tmp/FamilyGuard.msi"));

        _hashVerifier.VerifyAsync("/tmp/FamilyGuard.msi", _update.Sha256, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _useCase.ExecuteAsync(_update);

        result.ShouldBeFalse();
        await _installer.DidNotReceive().ApplyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _installer.Received().CleanupTempFiles();
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.UpdateFailed &&
            e.Details.ContainsKey("reason")));
    }

    [Fact]
    public async Task InstallFails_LogsFailure_WithExitCode()
    {
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(new UpdateDownloadResult(true, FilePath: "/tmp/FamilyGuard.msi"));

        _hashVerifier.VerifyAsync("/tmp/FamilyGuard.msi", _update.Sha256, Arg.Any<CancellationToken>())
            .Returns(true);

        _installer.ApplyAsync("/tmp/FamilyGuard.msi", Arg.Any<CancellationToken>())
            .Returns(new UpdateApplyResult(false, ExitCode: 1603, Error: "Fatal error during installation"));

        var result = await _useCase.ExecuteAsync(_update);

        result.ShouldBeFalse();
        _installer.Received().CleanupTempFiles();
        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.UpdateFailed &&
            e.Details.ContainsKey("exit_code")));
    }

    [Fact]
    public async Task ConcurrentCall_ReturnsImmediately()
    {
        // Simulate a slow download
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(500);
                return new UpdateDownloadResult(true, FilePath: "/tmp/FamilyGuard.msi");
            });
        _hashVerifier.VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _installer.ApplyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateApplyResult(true));

        // Start first call
        var first = _useCase.ExecuteAsync(_update);

        // Second call while first is in progress
        var second = await _useCase.ExecuteAsync(_update);

        second.ShouldBeFalse(); // Rejected — already in progress

        await first; // Let first complete
    }

    [Fact]
    public async Task LogsUpdateAvailable_WithVersionDetails()
    {
        _installer.DownloadAsync(_update, Arg.Any<CancellationToken>())
            .Returns(new UpdateDownloadResult(false, Error: "fail"));

        await _useCase.ExecuteAsync(_update);

        _eventStore.Received().Append(Arg.Is<StructuredEvent>(e =>
            e.EventType == EventType.UpdateAvailable &&
            e.Details["version"] == "1.0.0"));
    }
}
