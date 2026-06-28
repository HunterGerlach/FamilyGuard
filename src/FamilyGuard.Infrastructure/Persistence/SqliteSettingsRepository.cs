using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using FamilyGuard.Application.Ports.Output;

namespace FamilyGuard.Infrastructure.Persistence;

public sealed class SqliteSettingsRepository : ISettingsRepository, IDisposable
{
    private const string PinHashPrefix = "pbkdf2-sha256";
    private const int PinHashIterations = 210_000;
    private const int PinSaltSizeBytes = 16;
    private const int PinHashSizeBytes = 32;

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
        var settings = json is null
            ? new ProtectedSettings()
            : JsonSerializer.Deserialize<ProtectedSettings>(json) ?? new ProtectedSettings();

        settings.PinHash = GetValue("pin_hash") ?? settings.PinHash;
        return settings;
    }

    public void Save(ProtectedSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        SetValue("protected_settings", json);
    }

    public void SetPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN cannot be empty.", nameof(pin));

        var hash = HashPin(pin);
        SetValue("pin_hash", hash);
    }

    public bool VerifyPin(string pin)
    {
        var storedHash = GetValue("pin_hash");
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (VerifyPbkdf2Hash(pin, storedHash))
            return true;

        if (!IsLegacySha256Hash(storedHash) || !VerifyLegacySha256Hash(pin, storedHash))
            return false;

        SetValue("pin_hash", HashPin(pin));
        return true;
    }

    private static string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(PinSaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            PinHashIterations,
            HashAlgorithmName.SHA256,
            PinHashSizeBytes);

        return string.Join('$',
            PinHashPrefix,
            PinHashIterations.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    private static bool VerifyPbkdf2Hash(string pin, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != PinHashPrefix)
            return false;

        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                pin,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsLegacySha256Hash(string storedHash)
    {
        try
        {
            return Convert.FromBase64String(storedHash).Length == PinHashSizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool VerifyLegacySha256Hash(string pin, string storedHash)
    {
        var inputHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(pin)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(inputHash));
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
