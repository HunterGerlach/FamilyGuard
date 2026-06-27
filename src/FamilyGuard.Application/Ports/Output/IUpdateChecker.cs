namespace FamilyGuard.Application.Ports.Output;

public interface IUpdateChecker
{
    Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, CancellationToken ct = default);
}

public sealed record UpdateInfo(
    string Version,
    string Sha256,
    string DownloadUrl,
    DateTimeOffset ReleasedAt);
