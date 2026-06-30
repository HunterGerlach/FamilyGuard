using Shouldly;
using Xunit;
using Microsoft.Data.Sqlite;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.Infrastructure.Tests.Persistence;

public class SqlitePolicyRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqlitePolicyRepository _repo;

    public SqlitePolicyRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Run migrations to create schema
        var runner = new MigrationRunner(_connection);
        runner.Run(Migrations.All);

        _repo = new SqlitePolicyRepository(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void Save_ThenLoadAll_RoundTrips()
    {
        var rule = new PolicyRule(
            id: "test_rule",
            name: "Test Rule",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions: [new PolicyAction(PolicyActionType.MuteMicrophone)],
            appliesToUsers: ["child1"]);

        _repo.Save(rule);
        var loaded = _repo.LoadAll();

        loaded.Count.ShouldBe(1);
        loaded[0].Id.ShouldBe("test_rule");
        loaded[0].Name.ShouldBe("Test Rule");
        loaded[0].Enabled.ShouldBeTrue();
        loaded[0].Conditions.Count.ShouldBe(2);
        loaded[0].Actions.Count.ShouldBe(1);
        loaded[0].AppliesToUser("child1").ShouldBeTrue();
        loaded[0].AppliesToUser("parent").ShouldBeFalse();
    }

    [Fact]
    public void Save_ExistingRule_Updates()
    {
        var rule1 = new PolicyRule("test", "Test", true);
        _repo.Save(rule1);

        var rule2 = new PolicyRule("test", "Updated Test", false);
        _repo.Save(rule2);

        var loaded = _repo.LoadAll();
        loaded.Count.ShouldBe(1);
        loaded[0].Name.ShouldBe("Updated Test");
        loaded[0].Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Delete_RemovesRule()
    {
        _repo.Save(new PolicyRule("test", "Test", true));
        _repo.Delete("test");

        _repo.LoadAll().ShouldBeEmpty();
    }

    [Fact]
    public void EnsureDefaultRules_CreatesV1MuteRule()
    {
        _repo.EnsureDefaultRules();

        var rules = _repo.LoadAll();
        rules.Count.ShouldBe(1);
        rules[0].Id.ShouldBe("mute_unattended_microphone");
        rules[0].Enabled.ShouldBeTrue();
        rules[0].Conditions.ShouldContain(PolicyCondition.MicUnmuted);
        rules[0].Conditions.ShouldContain(PolicyCondition.PresenceAway);
    }

    [Fact]
    public void EnsureDefaultRules_DoesNotDuplicateIfExists()
    {
        _repo.EnsureDefaultRules();
        _repo.EnsureDefaultRules();

        _repo.LoadAll().Count.ShouldBe(1);
    }

    [Fact]
    public void LoadAll_EmptyWhenNoRules()
    {
        _repo.LoadAll().ShouldBeEmpty();
    }
}
