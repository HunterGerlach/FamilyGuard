using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using FamilyGuard.Application.Ports.Output;

namespace FamilyGuard.Infrastructure.Persistence;

public sealed class SqliteSettingsRepository : ISettingsRepository, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteSettingsRepository(string connectionString)
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public ProtectedSettings Load()
    {
        var json = GetValue("protected_settings");
        if (json is null)
            return new ProtectedSettings();

        return JsonSerializer.Deserialize<ProtectedSettings>(json)
            ?? new ProtectedSettings();
    }

    public void Save(ProtectedSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        SetValue("protected_settings", json);
    }

    public void SetPin(string pin)
    {
        var hash = HashPin(pin);
        SetValue("pin_hash", hash);
    }

    public bool VerifyPin(string pin)
    {
        var storedHash = GetValue("pin_hash");
        if (storedHash is null)
            return false;

        var inputHash = HashPin(pin);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(inputHash));
    }

    private static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
        return Convert.ToBase64String(bytes);
    }

    private string? GetValue(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        return cmd.ExecuteScalar() as string;
    }

    private void SetValue(string key, string value)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = @value
            """;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
