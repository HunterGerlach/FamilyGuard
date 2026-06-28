namespace FamilyGuard.Application.Ports.Output;

/// <summary>
/// Downloads and applies updates. Each method has a single responsibility:
/// DownloadAsync handles retrieval with retry; ApplyAsync handles installation.
/// </summary>
public interface IUpdateInstaller
{
    /// <summary>
    /// Downloads the update artifact to a local temp path.
    /// Retries with exponential backoff on transient failures.
    /// </summary>
    Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, CancellationToken ct = default);

    /// <summary>
    /// Applies the downloaded MSI via msiexec. The MSI's ServiceControl
    /// element handles stopping/restarting the Windows service.
    /// </summary>
    Task<UpdateApplyResult> ApplyAsync(string msiPath, CancellationToken ct = default);

    /// <summary>
    /// Cleans up temporary files from previous download attempts.
    /// Called on service startup and after failed installs.
    /// </summary>
    void CleanupTempFiles();
}

public sealed record UpdateDownloadResult(bool Success, string? FilePath = null, string? Error = null);

public sealed record UpdateApplyResult(bool Success, int ExitCode = 0, string? Error = null);
