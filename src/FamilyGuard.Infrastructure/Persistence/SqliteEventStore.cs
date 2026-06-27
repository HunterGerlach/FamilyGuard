using System.Text.Json;
using Microsoft.Data.Sqlite;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Infrastructure.Persistence;

public sealed class SqliteEventStore : IEventStore, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteEventStore(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS events (
                id TEXT PRIMARY KEY,
                timestamp_utc TEXT NOT NULL,
                event_type TEXT NOT NULL,
                windows_user TEXT NOT NULL,
                session_id INTEGER NOT NULL,
                policy_id TEXT,
                details_json TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_events_timestamp ON events(timestamp_utc);
            CREATE INDEX IF NOT EXISTS idx_events_type ON events(event_type);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Append(StructuredEvent structuredEvent)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO events (id, timestamp_utc, event_type, windows_user, session_id, policy_id, details_json)
            VALUES (@id, @ts, @type, @user, @session, @policy, @details)
            """;
        cmd.Parameters.AddWithValue("@id", structuredEvent.Id.ToString());
        cmd.Parameters.AddWithValue("@ts", structuredEvent.TimestampUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@type", structuredEvent.EventType.ToString());
        cmd.Parameters.AddWithValue("@user", structuredEvent.WindowsUser);
        cmd.Parameters.AddWithValue("@session", structuredEvent.SessionId.Value);
        cmd.Parameters.AddWithValue("@policy", (object?)structuredEvent.PolicyId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@details",
            structuredEvent.Details.Count > 0
                ? JsonSerializer.Serialize(structuredEvent.Details)
                : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<StructuredEvent> QueryByTimeRange(DateTimeOffset from, DateTimeOffset to)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, timestamp_utc, event_type, windows_user, session_id, policy_id, details_json
            FROM events
            WHERE timestamp_utc >= @from AND timestamp_utc <= @to
            ORDER BY timestamp_utc DESC
            """;
        cmd.Parameters.AddWithValue("@from", from.ToString("O"));
        cmd.Parameters.AddWithValue("@to", to.ToString("O"));
        return ReadEvents(cmd);
    }

    public IReadOnlyList<StructuredEvent> QueryByEventType(EventType eventType, int limit = 100)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, timestamp_utc, event_type, windows_user, session_id, policy_id, details_json
            FROM events
            WHERE event_type = @type
            ORDER BY timestamp_utc DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@type", eventType.ToString());
        cmd.Parameters.AddWithValue("@limit", limit);
        return ReadEvents(cmd);
    }

    public IReadOnlyList<StructuredEvent> QueryRecent(int limit = 50)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, timestamp_utc, event_type, windows_user, session_id, policy_id, details_json
            FROM events
            ORDER BY timestamp_utc DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);
        return ReadEvents(cmd);
    }

    private static List<StructuredEvent> ReadEvents(SqliteCommand cmd)
    {
        var results = new List<StructuredEvent>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var detailsJson = reader.IsDBNull(6) ? null : reader.GetString(6);
            var details = detailsJson is not null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(detailsJson)
                : null;

            results.Add(StructuredEvent.Create(
                eventType: Enum.Parse<EventType>(reader.GetString(2)),
                windowsUser: reader.GetString(3),
                sessionId: new SessionId(reader.GetInt32(4)),
                policyId: reader.IsDBNull(5) ? null : reader.GetString(5),
                timestamp: DateTimeOffset.Parse(reader.GetString(1)),
                details: details));
        }
        return results;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
