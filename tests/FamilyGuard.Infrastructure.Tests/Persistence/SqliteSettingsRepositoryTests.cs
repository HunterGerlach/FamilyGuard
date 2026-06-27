using FluentAssertions;
using Xunit;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.Infrastructure.Tests.Persistence;

public class SqliteSettingsRepositoryTests : IDisposable
{
    private readonly SqliteSettingsRepository _repo;

    public SqliteSettingsRepositoryTests()
    {
        _repo = new SqliteSettingsRepository("Data Source=:memory:");
    }

    public void Dispose()
    {
        _repo.Dispose();
    }

    [Fact]
    public void Load_WithNoSavedSettings_ReturnsDefaults()
    {
        var settings = _repo.Load();

        settings.PresenceTimeoutSeconds.Should().Be(90);
        settings.CoveredUsers.Should().BeEmpty();
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var settings = new ProtectedSettings
        {
            PresenceTimeoutSeconds = 120,
            CoveredUsers = ["child1", "child2"]
        };

        _repo.Save(settings);
        var loaded = _repo.Load();

        loaded.PresenceTimeoutSeconds.Should().Be(120);
        loaded.CoveredUsers.Should().BeEquivalentTo(["child1", "child2"]);
    }

    [Fact]
    public void Save_UpdatesThenLoad_ReturnsLatest()
    {
        var settings1 = new ProtectedSettings { PresenceTimeoutSeconds = 90 };
        _repo.Save(settings1);

        var settings2 = new ProtectedSettings { PresenceTimeoutSeconds = 120 };
        _repo.Save(settings2);

        var loaded = _repo.Load();
        loaded.PresenceTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void SetPin_ThenVerify_Succeeds()
    {
        _repo.SetPin("1234");

        _repo.VerifyPin("1234").Should().BeTrue();
    }

    [Fact]
    public void SetPin_ThenVerifyWrongPin_Fails()
    {
        _repo.SetPin("1234");

        _repo.VerifyPin("5678").Should().BeFalse();
    }

    [Fact]
    public void VerifyPin_WithNoPin_ReturnsFalse()
    {
        _repo.VerifyPin("1234").Should().BeFalse();
    }
}
