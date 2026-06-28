using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Updates;

namespace FamilyGuard.Infrastructure.Tests.Updates;

public class Sha256HashVerifierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Sha256HashVerifier _verifier = new();

    public Sha256HashVerifierTests()
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
    public async Task MatchingHash_ReturnsTrue()
    {
        var content = "test file content for hashing"u8.ToArray();
        var filePath = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllBytesAsync(filePath, content);

        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(content));

        var result = await _verifier.VerifyAsync(filePath, expectedHash);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task MismatchingHash_ReturnsFalse()
    {
        var filePath = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllTextAsync(filePath, "some content");

        var result = await _verifier.VerifyAsync(filePath, "0000000000000000000000000000000000000000000000000000000000000000");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CaseInsensitive_BothUpperAndLowerMatch()
    {
        var content = "case test"u8.ToArray();
        var filePath = Path.Combine(_tempDir, "test.msi");
        await File.WriteAllBytesAsync(filePath, content);

        var hashLower = Convert.ToHexStringLower(SHA256.HashData(content));
        var hashUpper = hashLower.ToUpperInvariant();

        (await _verifier.VerifyAsync(filePath, hashLower)).ShouldBeTrue();
        (await _verifier.VerifyAsync(filePath, hashUpper)).ShouldBeTrue();
    }

    [Fact]
    public async Task MissingFile_ReturnsFalse()
    {
        var result = await _verifier.VerifyAsync(
            Path.Combine(_tempDir, "nonexistent.msi"),
            "0000000000000000000000000000000000000000000000000000000000000000");

        result.ShouldBeFalse();
    }
}
