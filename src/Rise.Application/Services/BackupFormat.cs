using System.Text.Json;
using System.Text.Json.Nodes;
using Rise.Domain;

namespace Rise.Application.Services;

/// <summary>
/// The versioned backup format, and the migration engine. A backup
/// file is a JSON envelope — a format version, an export date, and
/// the saved state — so an old file can be brought forward instead
/// of rejected. The state shape for the current version is the DTO
/// set in <see cref="BackupStateDto"/> (documented in
/// MIGRATIONS.md); migrations are stepwise, forward-only functions
/// from one version's state JSON to the next, registered in
/// <see cref="Migrations"/> and never edited after they ship —
/// only appended. Migrating normalizes a file to the target
/// version's schema: fields the target version doesn't know are
/// dropped, never carried along.
/// </summary>
public static class BackupFormat
{
    public const int CurrentVersion = 1;

    private static readonly Dictionary<int, Func<JsonObject, JsonObject>> Migrations = new();

    /// <summary>
    /// Reads a backup file: it must be a JSON object with an integer
    /// format version (1 through the current version), a non-blank
    /// export date, and a state object. Anything else fails with a
    /// human-readable reason; nothing is thrown.
    /// </summary>
    public static Result<BackupEnvelope> Parse(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return Result<BackupEnvelope>.Failure("That file isn't valid JSON.");
        }

        if (node is not JsonObject envelope)
        {
            return Result<BackupEnvelope>.Failure("That file isn't a Rise backup (expected a JSON object).");
        }

        if (!envelope.TryGetPropertyValue("FormatVersion", out JsonNode? versionNode)
            || versionNode is not JsonValue versionValue
            || !versionValue.TryGetValue<int>(out int version))
        {
            return Result<BackupEnvelope>.Failure("That file has no backup version, so it can't be migrated or imported.");
        }

        if (version < 1)
        {
            return Result<BackupEnvelope>.Failure("That file's backup version isn't valid.");
        }

        if (version > CurrentVersion)
        {
            return Result<BackupEnvelope>.Failure(
                $"That backup is from a newer version (v{version}) than this app supports (v{CurrentVersion}).");
        }

        if (!envelope.TryGetPropertyValue("ExportedAt", out JsonNode? exportedNode)
            || exportedNode is not JsonValue exportedValue
            || !exportedValue.TryGetValue<string>(out string? exportedAt)
            || string.IsNullOrWhiteSpace(exportedAt))
        {
            return Result<BackupEnvelope>.Failure("That file has no export date.");
        }

        if (!envelope.TryGetPropertyValue("State", out JsonNode? stateNode) || stateNode is not JsonObject state)
        {
            return Result<BackupEnvelope>.Failure("That file has no saved state in it.");
        }

        return Result<BackupEnvelope>.Success(new BackupEnvelope(version, exportedAt, state));
    }

    /// <summary>
    /// Brings a parsed backup to the target version, one registered
    /// step at a time. Forward-only: a target older than the file's
    /// version, newer than the current version, or missing a
    /// registered step fails with a reason. Migrating to the file's
    /// own version is an identity.
    /// </summary>
    public static Result<BackupEnvelope> Migrate(BackupEnvelope envelope, int targetVersion)
    {
        if (targetVersion > CurrentVersion)
        {
            return Result<BackupEnvelope>.Failure(
                $"Version {targetVersion} is newer than this app supports (v{CurrentVersion}).");
        }

        if (targetVersion < envelope.FormatVersion)
        {
            return Result<BackupEnvelope>.Failure("A backup can only migrate forward, not back.");
        }

        JsonObject state = envelope.State;
        for (int version = envelope.FormatVersion; version < targetVersion; version++)
        {
            if (!Migrations.TryGetValue(version, out Func<JsonObject, JsonObject>? migrate))
            {
                return Result<BackupEnvelope>.Failure($"No migration from v{version} to v{version + 1} is registered.");
            }

            state = migrate(state);
        }

        return Result<BackupEnvelope>.Success(new BackupEnvelope(targetVersion, envelope.ExportedAt, state));
    }

    /// <summary>Serializes an envelope to indented JSON for download.</summary>
    public static string Serialize(BackupEnvelope envelope)
    {
        var root = new JsonObject
        {
            ["FormatVersion"] = envelope.FormatVersion,
            ["ExportedAt"] = envelope.ExportedAt,
            ["State"] = envelope.State.DeepClone(),
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
