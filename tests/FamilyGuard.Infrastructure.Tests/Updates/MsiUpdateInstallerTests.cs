using System.Net;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Updates;

namespace FamilyGuard.Infrastructure.Tests.Updates;

public class MsiUpdateInstallerTests : IDisposable
{
    private readonly string _tempDir;

    public MsiUpdateInstallerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fg-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task DownloadAsync_Success_ReturnsFilePath()
    {
        var content = "fake MSI content"u8.ToArray();
        var handler = new FakeHttpHandler(HttpStatusCode.OK, content);
        var http = new HttpClient(handler);
        var installer = new MsiUpdateInstaller(http, _tempDir,
            NullLogger<MsiUpdateInstaller>.Instance);

        var update = new UpdateInfo("1.0.0", "abc", "https://example.com/FamilyGuard.msi",
            DateTimeOffset.UtcNow);

        var result = await installer.DownloadAsync(update);

        result.Success.ShouldBeTrue();
        result.FilePath.ShouldNotBeNull();
        File.Exists(result.FilePath).ShouldBeTrue();
        (await File.ReadAllBytesAsync(result.FilePath)).ShouldBe(content);
    }

    [Fact]
    public async Task DownloadAsync_HttpError_RetriesThenFails()
    {
        var handler = new CountingFakeHandler(HttpStatusCode.InternalServerError, "error");
        var http = new HttpClient(handler);
        var installer = new MsiUpdateInstaller(http, _tempDir,
            NullLogger<MsiUpdateInstaller>.Instance);

        var update = new UpdateInfo("1.0.0", "abc", "https://example.com/FamilyGuard.msi",
            DateTimeOffset.UtcNow);

        var result = await installer.DownloadAsync(update);

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        handler.CallCount.ShouldBe(3); // 3 retries
    }

    [Fact]
    public async Task DownloadAsync_FirstFailsThenSucceeds()
    {
        var content = "MSI bytes"u8.ToArray();
        var handler = new FailThenSucceedHandler(1, content);
        var http = new HttpClient(handler);
        var installer = new MsiUpdateInstaller(http, _tempDir,
            NullLogger<MsiUpdateInstaller>.Instance);

        var update = new UpdateInfo("1.0.0", "abc", "https://example.com/FamilyGuard.msi",
            DateTimeOffset.UtcNow);

        var result = await installer.DownloadAsync(update);

        result.Success.ShouldBeTrue();
        result.FilePath.ShouldNotBeNull();
    }

    [Fact]
    public void CleanupTempFiles_RemovesMsiFiles()
    {
        var msiPath = Path.Combine(_tempDir, "FamilyGuard-update.msi");
        File.WriteAllText(msiPath, "temp");

        var installer = new MsiUpdateInstaller(new HttpClient(), _tempDir,
            NullLogger<MsiUpdateInstaller>.Instance);

        installer.CleanupTempFiles();

        File.Exists(msiPath).ShouldBeFalse();
    }

    // --- Test Helpers ---

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly byte[] _content;

        public FakeHttpHandler(HttpStatusCode code, byte[] content)
        {
            _code = code;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromResult(new HttpResponseMessage(_code)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }

    private sealed class CountingFakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _content;
        public int CallCount { get; private set; }

        public CountingFakeHandler(HttpStatusCode code, string content)
        {
            _code = code;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(_code)
            {
                Content = new StringContent(_content)
            });
        }
    }

    private sealed class FailThenSucceedHandler : HttpMessageHandler
    {
        private readonly int _failCount;
        private readonly byte[] _content;
        private int _calls;

        public FailThenSucceedHandler(int failCount, byte[] content)
        {
            _failCount = failCount;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            _calls++;
            if (_calls <= _failCount)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }
}
