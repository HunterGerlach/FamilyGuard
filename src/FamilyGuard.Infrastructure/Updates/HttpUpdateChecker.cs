using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyGuard.Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Updates;

public sealed class HttpUpdateChecker : IUpdateChecker
{
    private readonly HttpClient _http;
    private readonly string _manifestUrl;
    private readonly ILogger<HttpUpdateChecker> _logger;

    public HttpUpdateChecker(HttpClient http, string manifestUrl, ILogger<HttpUpdateChecker> logger)
    {
        _http = http;
        _manifestUrl = manifestUrl;
        _logger = logger;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Checking for updates at {Url}", _manifestUrl);

            var response = await _http.GetAsync(_manifestUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update check failed: HTTP {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json);
            if (manifest is null)
            {
                _logger.LogWarning("Update manifest was null or invalid");
                return null;
            }

            if (!IsNewerVersion(manifest.Version, currentVersion))
            {
                _logger.LogDebug("Current version {Current} is up to date (latest: {Latest})",
                    currentVersion, manifest.Version);
                return null;
            }

            _logger.LogInformation("Update available: {Version}", manifest.Version);
            return new UpdateInfo(
                manifest.Version,
                manifest.Sha256,
                manifest.DownloadUrl,
                manifest.ReleasedAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return null;
        }
    }

    private static bool IsNewerVersion(string remote, string current)
    {
        if (Version.TryParse(remote, out var remoteVer) &&
            Version.TryParse(current, out var currentVer))
        {
            return remoteVer > currentVer;
        }
        return false;
    }

    private sealed class UpdateManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("released_at")]
        public DateTimeOffset ReleasedAt { get; set; }
    }
}
