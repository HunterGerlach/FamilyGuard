using System.Text.Json;
using Microsoft.Data.Sqlite;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Infrastructure.Persistence;

public sealed class SqlitePolicyRepository : IPolicyRepository
{
    private readonly SqliteConnection _connection;

    public SqlitePolicyRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public IReadOnlyList<PolicyRule> LoadAll()
    {
        var rules = new List<PolicyRule>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, enabled, conditions_json, actions_json, applies_to_users_json
            FROM policy_rules
            ORDER BY id
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var conditions = JsonSerializer.Deserialize<List<PolicyCondition>>(reader.GetString(3)) ?? [];
            var actions = JsonSerializer.Deserialize<List<PolicyAction>>(reader.GetString(4)) ?? [];
            var users = reader.IsDBNull(5)
                ? null
                : JsonSerializer.Deserialize<List<string>>(reader.GetString(5));

            rules.Add(new PolicyRule(
                id: reader.GetString(0),
                name: reader.GetString(1),
                enabled: reader.GetInt32(2) != 0,
                conditions: conditions,
                actions: actions,
                appliesToUsers: users));
        }

        return rules;
    }

    public void Save(PolicyRule rule)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO policy_rules (id, name, enabled, conditions_json, actions_json, applies_to_users_json, created_at, updated_at)
            VALUES (@id, @name, @enabled, @conditions, @actions, @users, @now, @now)
            ON CONFLICT(id) DO UPDATE SET
                name = @name,
                enabled = @enabled,
                conditions_json = @conditions,
                actions_json = @actions,
                applies_to_users_json = @users,
                updated_at = @now
            """;
        cmd.Parameters.AddWithValue("@id", rule.Id);
        cmd.Parameters.AddWithValue("@name", rule.Name);
        cmd.Parameters.AddWithValue("@enabled", rule.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@conditions", JsonSerializer.Serialize(rule.Conditions));
        cmd.Parameters.AddWithValue("@actions", JsonSerializer.Serialize(rule.Actions));
        cmd.Parameters.AddWithValue("@users", SerializeUsers(rule));
        cmd.Parameters.AddWithValue("@now", now);
        cmd.ExecuteNonQuery();
    }

    public void Delete(string ruleId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM policy_rules WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", ruleId);
        cmd.ExecuteNonQuery();
    }

    public void EnsureDefaultRules()
    {
        var existing = LoadAll();
        if (existing.Any(r => r.Id == "mute_unattended_microphone"))
            return;

        Save(new PolicyRule(
            id: "mute_unattended_microphone",
            name: "Mute unattended microphone",
            enabled: true,
            conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
            actions:
            [
                new PolicyAction(PolicyActionType.MuteMicrophone),
                new PolicyAction(PolicyActionType.NotifyChild,
                    "Microphone was muted because nobody appeared active at the computer."),
                new PolicyAction(PolicyActionType.LogEvent)
            ]));
    }

    private static object SerializeUsers(PolicyRule rule)
    {
        if (rule.AppliesToUsers is null or { Count: 0 })
            return DBNull.Value;

        return JsonSerializer.Serialize(rule.AppliesToUsers);
    }
}
