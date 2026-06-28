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

public static class UpdateDefaults
{
    /// <summary>
    /// Default update channel URL — GitHub latest release manifest.
    /// 12-Factor: this is the default; actual value comes from settings.
    /// </summary>
    public const string StableChannelUrl =
        "https://github.com/HunterGerlach/FamilyGuard/releases/latest/download/update-manifest.json";
}
