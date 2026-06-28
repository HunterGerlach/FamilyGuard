using System.Security.Cryptography;
using FamilyGuard.Application.Ports.Output;

namespace FamilyGuard.Infrastructure.Updates;

public sealed class Sha256HashVerifier : IHashVerifier
{
    public async Task<bool> VerifyAsync(string filePath, string expectedSha256Hex, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return false;

        var actualHash = await ComputeSha256Async(filePath, ct);
        var expectedBytes = Convert.FromHexString(expectedSha256Hex);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedBytes);
    }

    private static async Task<byte[]> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        return await SHA256.HashDataAsync(stream, ct);
    }
}
