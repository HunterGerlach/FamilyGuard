using System.Net;
using System.Text.Json;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using FamilyGuard.Infrastructure.Updates;

namespace FamilyGuard.Infrastructure.Tests.Updates;

public class HttpUpdateCheckerTests
{
    [Fact]
    public async Task CheckForUpdate_NewerVersion_ReturnsUpdateInfo()
    {
        var manifest = new
        {
            version = "1.0.0",
            sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            download_url = "https://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldNotBeNull();
        result.Version.ShouldBe("1.0.0");
        result.Sha256.ShouldBe("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
    }

    [Fact]
    public async Task CheckForUpdate_SameVersion_ReturnsNull()
    {
        var manifest = new
        {
            version = "1.0.0",
            sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            download_url = "https://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("1.0.0");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckForUpdate_HttpError_ReturnsNull()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "error");
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckForUpdate_OlderVersion_ReturnsNull()
    {
        var manifest = new
        {
            version = "0.8.0",
            sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            download_url = "https://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldBeNull();
    }


    [Fact]
    public async Task CheckForUpdate_InsecureManifestUrl_ReturnsNull()
    {
        var manifest = new
        {
            version = "1.0.0",
            sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            download_url = "https://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "http://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckForUpdate_InsecureDownloadUrl_ReturnsNull()
    {
        var manifest = new
        {
            version = "1.0.0",
            sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            download_url = "http://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckForUpdate_InvalidSha256_ReturnsNull()
    {
        var manifest = new
        {
            version = "1.0.0",
            sha256 = "abc123",
            download_url = "https://example.com/update.msi",
            released_at = DateTimeOffset.UtcNow
        };

        var handler = new FakeHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(manifest));
        var http = new HttpClient(handler);
        var checker = new HttpUpdateChecker(http, "https://example.com/manifest.json",
            NullLogger<HttpUpdateChecker>.Instance);

        var result = await checker.CheckForUpdateAsync("0.9.0");

        result.ShouldBeNull();
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public FakeHttpHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            });
        }
    }
}
