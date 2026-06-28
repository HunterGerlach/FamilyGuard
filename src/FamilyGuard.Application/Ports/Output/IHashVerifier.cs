namespace FamilyGuard.Application.Ports.Output;

/// <summary>
/// Verifies file integrity via cryptographic hash comparison.
/// Separated from IUpdateInstaller per Interface Segregation Principle.
/// </summary>
public interface IHashVerifier
{
    /// <summary>
    /// Computes the SHA256 hash of the file at the given path and compares
    /// it to the expected hash using constant-time comparison.
    /// </summary>
    Task<bool> VerifyAsync(string filePath, string expectedSha256Hex, CancellationToken ct = default);
}
