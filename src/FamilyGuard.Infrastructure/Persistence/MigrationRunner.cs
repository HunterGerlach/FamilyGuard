using Microsoft.Data.Sqlite;

namespace FamilyGuard.Infrastructure.Persistence;

public sealed record Migration(int Version, string Sql);

public sealed record AppliedMigration(int Version, DateTimeOffset AppliedAt);

public sealed class MigrationRunner
{
    private readonly SqliteConnection _connection;

    public int CurrentVersion { get; private set; }

    public MigrationRunner(SqliteConnection connection)
    {
        _connection = connection;
    }

    public void Run(IReadOnlyList<Migration> migrations)
    {
        EnsureSchemaVersionTable();
        CurrentVersion = GetCurrentVersion();

        foreach (var migration in migrations.OrderBy(m => m.Version))
        {
            if (migration.Version <= CurrentVersion)
                continue;

            using var transaction = _connection.BeginTransaction();
            try
            {
                using var cmd = _connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = migration.Sql;
                cmd.ExecuteNonQuery();

                RecordMigration(migration.Version, transaction);
                transaction.Commit();
                CurrentVersion = migration.Version;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public IReadOnlyList<AppliedMigration> GetAppliedMigrations()
    {
        EnsureSchemaVersionTable();

        var results = new List<AppliedMigration>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT version, applied_at FROM schema_version ORDER BY version";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new AppliedMigration(
                reader.GetInt32(0),
                DateTimeOffset.Parse(reader.GetString(1))));
        }
        return results;
    }

    private void EnsureSchemaVersionTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER PRIMARY KEY,
                applied_at TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    private int GetCurrentVersion()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void RecordMigration(int version, SqliteTransaction transaction)
    {
        using var cmd = _connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO schema_version (version, applied_at) VALUES (@v, @t)";
        cmd.Parameters.AddWithValue("@v", version);
        cmd.Parameters.AddWithValue("@t", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }
}
