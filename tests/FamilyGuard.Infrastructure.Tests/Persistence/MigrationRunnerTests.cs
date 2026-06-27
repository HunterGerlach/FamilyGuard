using Shouldly;
using Xunit;
using Microsoft.Data.Sqlite;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.Infrastructure.Tests.Persistence;

public class MigrationRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MigrationRunner _runner;

    public MigrationRunnerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _runner = new MigrationRunner(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void Run_CreatesSchemaVersionTable()
    {
        _runner.Run([]);

        TableExists("schema_version").ShouldBeTrue();
    }

    [Fact]
    public void Run_ExecutesMigrationInOrder()
    {
        var migrations = new[]
        {
            new Migration(1, "CREATE TABLE test1 (id INTEGER PRIMARY KEY)"),
            new Migration(2, "CREATE TABLE test2 (id INTEGER PRIMARY KEY)")
        };

        _runner.Run(migrations);

        TableExists("test1").ShouldBeTrue();
        TableExists("test2").ShouldBeTrue();
        _runner.CurrentVersion.ShouldBe(2);
    }

    [Fact]
    public void Run_SkipsAlreadyAppliedMigrations()
    {
        var migrations1 = new[]
        {
            new Migration(1, "CREATE TABLE test1 (id INTEGER PRIMARY KEY)")
        };
        _runner.Run(migrations1);

        var migrations2 = new[]
        {
            new Migration(1, "CREATE TABLE test1 (id INTEGER PRIMARY KEY)"),
            new Migration(2, "CREATE TABLE test2 (id INTEGER PRIMARY KEY)")
        };
        _runner.Run(migrations2);

        TableExists("test2").ShouldBeTrue();
        _runner.CurrentVersion.ShouldBe(2);
    }

    [Fact]
    public void CurrentVersion_ZeroWhenNoMigrations()
    {
        _runner.Run([]);

        _runner.CurrentVersion.ShouldBe(0);
    }

    [Fact]
    public void Run_RecordsAppliedTimestamp()
    {
        var migrations = new[]
        {
            new Migration(1, "CREATE TABLE test1 (id INTEGER PRIMARY KEY)")
        };

        _runner.Run(migrations);

        var appliedMigrations = _runner.GetAppliedMigrations();
        appliedMigrations.Count.ShouldBe(1);
        appliedMigrations[0].Version.ShouldBe(1);
        appliedMigrations[0].AppliedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    private bool TableExists(string tableName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
