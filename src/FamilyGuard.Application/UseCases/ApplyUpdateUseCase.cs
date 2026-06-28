using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.UseCases;

/// <summary>
/// Process Manager (EIP): orchestrates the multi-step update workflow.
/// Each step emits a structured event. On failure at any step, logs
/// UpdateFailed with details and performs cleanup. Concurrent calls
/// are rejected via an atomic in-progress guard.
/// </summary>
public sealed class ApplyUpdateUseCase
{
    private readonly IUpdateInstaller _installer;
    private readonly IHashVerifier _hashVerifier;
    private readonly IEventStore _eventStore;
    private readonly SuperviseSessionsUseCase _supervisor;

    private int _updateInProgress;

    public ApplyUpdateUseCase(
        IUpdateInstaller installer,
        IHashVerifier hashVerifier,
        IEventStore eventStore,
        SuperviseSessionsUseCase supervisor)
    {
        _installer = installer;
        _hashVerifier = hashVerifier;
        _eventStore = eventStore;
        _supervisor = supervisor;
    }

    /// <summary>
    /// Executes the full update pipeline: download → verify hash → shutdown agents → apply MSI.
    /// Returns true if the update was successfully applied (service will be restarted by msiexec).
    /// Returns false on any failure or if another update is already in progress.
    /// </summary>
    public async Task<bool> ExecuteAsync(UpdateInfo update, CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _updateInProgress, 1, 0) != 0)
            return false;

        try
        {
            return await ExecuteUpdatePipeline(update, ct);
        }
        finally
        {
            Interlocked.Exchange(ref _updateInProgress, 0);
        }
    }

    private async Task<bool> ExecuteUpdatePipeline(UpdateInfo update, CancellationToken ct)
    {
        // Step 1: Log update available
        LogEvent(EventType.UpdateAvailable, new Dictionary<string, string>
        {
            ["version"] = update.Version,
            ["download_url"] = update.DownloadUrl
        });

        // Step 2: Download
        var downloadResult = await _installer.DownloadAsync(update, ct);
        if (!downloadResult.Success || downloadResult.FilePath is null)
        {
            LogFailure("download_failed", downloadResult.Error ?? "Unknown download error");
            return false;
        }

        // Step 3: Verify hash (hard stop on mismatch — possible MITM)
        var hashValid = await _hashVerifier.VerifyAsync(downloadResult.FilePath, update.Sha256, ct);
        if (!hashValid)
        {
            LogFailure("hash_mismatch", "SHA256 hash does not match manifest. File deleted.");
            _installer.CleanupTempFiles();
            return false;
        }

        LogEvent(EventType.UpdateDownloaded, new Dictionary<string, string>
        {
            ["version"] = update.Version,
            ["file_path"] = downloadResult.FilePath
        });

        // Step 4: Shutdown all agents before install
        _supervisor.ShutdownAll();

        // Step 5: Apply MSI
        LogEvent(EventType.UpdateApplying, new Dictionary<string, string>
        {
            ["version"] = update.Version
        });

        var applyResult = await _installer.ApplyAsync(downloadResult.FilePath, ct);
        if (!applyResult.Success)
        {
            LogFailure("install_failed", applyResult.Error ?? "msiexec failed",
                new Dictionary<string, string> { ["exit_code"] = applyResult.ExitCode.ToString() });
            _installer.CleanupTempFiles();
            return false;
        }

        // Step 6: Success — msiexec will restart the service
        LogEvent(EventType.UpdateInstalled, new Dictionary<string, string>
        {
            ["version"] = update.Version
        });

        return true;
    }

    private void LogEvent(EventType type, Dictionary<string, string>? details = null)
    {
        _eventStore.Append(StructuredEvent.Create(
            eventType: type,
            windowsUser: "SYSTEM",
            sessionId: new SessionId(0),
            details: details));
    }

    private void LogFailure(string reason, string message, Dictionary<string, string>? extraDetails = null)
    {
        var details = new Dictionary<string, string>
        {
            ["reason"] = reason,
            ["message"] = message
        };

        if (extraDetails is not null)
        {
            foreach (var kvp in extraDetails)
                details[kvp.Key] = kvp.Value;
        }

        LogEvent(EventType.UpdateFailed, details);
    }
}
