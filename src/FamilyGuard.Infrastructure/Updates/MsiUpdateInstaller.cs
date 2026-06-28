using System.Diagnostics;
using FamilyGuard.Application.Ports.Output;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Updates;

/// <summary>
/// Downloads update MSIs via HttpClient with exponential backoff retry,
/// and applies them via msiexec. Single Responsibility: download + apply.
/// </summary>
public sealed class MsiUpdateInstaller : IUpdateInstaller
{
    private const int MaxRetries = 3;
    private const string TempFilePattern = "FamilyGuard-update*.msi";
    private static readonly TimeSpan MsiexecTimeout = TimeSpan.FromMinutes(5);

    private readonly HttpClient _http;
    private readonly string _tempDirectory;
    private readonly ILogger<MsiUpdateInstaller> _logger;

    public MsiUpdateInstaller(HttpClient http, string tempDirectory, ILogger<MsiUpdateInstaller> logger)
    {
        _http = http;
        _tempDirectory = tempDirectory;
        _logger = logger;
    }

    public async Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_tempDirectory);
        var targetPath = Path.Combine(_tempDirectory, $"FamilyGuard-update-{update.Version}.msi");

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Downloading update {Version} (attempt {Attempt}/{Max})",
                    update.Version, attempt, MaxRetries);

                var response = await _http.GetAsync(update.DownloadUrl, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Download attempt {Attempt} failed: HTTP {StatusCode}",
                        attempt, response.StatusCode);

                    if (attempt < MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    return new UpdateDownloadResult(false,
                        Error: $"HTTP {response.StatusCode} after {MaxRetries} attempts");
                }

                await using var fileStream = File.Create(targetPath);
                await response.Content.CopyToAsync(fileStream, ct);

                _logger.LogInformation("Downloaded {Version} to {Path}", update.Version, targetPath);
                return new UpdateDownloadResult(true, FilePath: targetPath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Download attempt {Attempt} failed", attempt);
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed after {Max} attempts", MaxRetries);
                return new UpdateDownloadResult(false, Error: ex.Message);
            }
        }

        return new UpdateDownloadResult(false, Error: "Exhausted retries");
    }

    public async Task<UpdateApplyResult> ApplyAsync(string msiPath, CancellationToken ct = default)
    {
        if (!File.Exists(msiPath))
            return new UpdateApplyResult(false, ExitCode: -1, Error: "MSI file not found");

        _logger.LogInformation("Applying update from {Path}", msiPath);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{msiPath}\" /quiet /norestart",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(MsiexecTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogError("msiexec timed out after {Timeout}", MsiexecTimeout);
                process.Kill(entireProcessTree: true);
                return new UpdateApplyResult(false, ExitCode: -1, Error: "msiexec timed out");
            }

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("msiexec failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, stderr);
                return new UpdateApplyResult(false, ExitCode: process.ExitCode, Error: stderr);
            }

            _logger.LogInformation("msiexec completed successfully");
            return new UpdateApplyResult(true, ExitCode: 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run msiexec");
            return new UpdateApplyResult(false, ExitCode: -1, Error: ex.Message);
        }
    }

    public void CleanupTempFiles()
    {
        if (!Directory.Exists(_tempDirectory))
            return;

        foreach (var file in Directory.GetFiles(_tempDirectory, TempFilePattern))
        {
            try
            {
                File.Delete(file);
                _logger.LogDebug("Cleaned up temp file: {Path}", file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp file: {Path}", file);
            }
        }
    }
}
