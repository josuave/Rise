using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Domain;
using Xunit;

namespace Rise.Core.Tests;

public class BackupFormatTests
{
    private static BackupEnvelope V1Envelope() => new(
        1,
        "2026-11-02T18:00:00.0000000+00:00",
        new JsonObject
        {
            ["Links"] = new JsonArray(),
            ["Theme"] = "system",
            ["Themes"] = new JsonArray(),
            ["Layouts"] = new JsonObject { ["ActiveId"] = "default", ["Layouts"] = new JsonArray() },
            ["WidgetConfigs"] = new JsonObject(),
        });

    [Fact]
    public void Parse_AcceptsWellFormedV1Envelope()
    {
        string json = BackupFormat.Serialize(V1Envelope());

        Result<BackupEnvelope> parsed = BackupFormat.Parse(json);

        Assert.True(parsed.IsSuccess);
        Assert.NotNull(parsed.Value);
        Assert.Equal(1, parsed.Value.FormatVersion);
    }

    [Fact]
    public void Parse_RejectsInvalidJson()
    {
        Result<BackupEnvelope> parsed = BackupFormat.Parse("not json");

        Assert.False(parsed.IsSuccess);
        Assert.NotNull(parsed.Error);
    }

    [Fact]
    public void Parse_RejectsNewerVersion()
    {
        JsonObject root = JsonNode.Parse(BackupFormat.Serialize(V1Envelope()))!.AsObject();
        root["FormatVersion"] = 2;

        Result<BackupEnvelope> parsed = BackupFormat.Parse(root.ToJsonString());

        Assert.False(parsed.IsSuccess);
        Assert.Contains("newer", parsed.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RejectsMissingState()
    {
        JsonObject root = JsonNode.Parse(BackupFormat.Serialize(V1Envelope()))!.AsObject();
        root.Remove("State");

        Result<BackupEnvelope> parsed = BackupFormat.Parse(root.ToJsonString());

        Assert.False(parsed.IsSuccess);
    }

    [Fact]
    public void Migrate_ToOwnVersion_IsIdentity()
    {
        BackupEnvelope envelope = V1Envelope();

        Result<BackupEnvelope> migrated = BackupFormat.Migrate(envelope, 1);

        Assert.True(migrated.IsSuccess);
        Assert.NotNull(migrated.Value);
        Assert.Equal(1, migrated.Value.FormatVersion);
        Assert.Equal(envelope.State.ToJsonString(), migrated.Value.State.ToJsonString());
    }

    [Fact]
    public void Migrate_Backwards_Fails()
    {
        Result<BackupEnvelope> migrated = BackupFormat.Migrate(V1Envelope(), 0);

        Assert.False(migrated.IsSuccess);
    }

    [Fact]
    public void Migrate_BeyondCurrent_Fails()
    {
        Result<BackupEnvelope> migrated = BackupFormat.Migrate(V1Envelope(), BackupFormat.CurrentVersion + 1);

        Assert.False(migrated.IsSuccess);
    }

    [Fact]
    public void Serialize_RoundTripsThroughParse()
    {
        BackupEnvelope envelope = V1Envelope();

        Result<BackupEnvelope> parsed = BackupFormat.Parse(BackupFormat.Serialize(envelope));

        Assert.True(parsed.IsSuccess);
        Assert.NotNull(parsed.Value);
        Assert.Equal(envelope.FormatVersion, parsed.Value.FormatVersion);
        Assert.Equal(envelope.ExportedAt, parsed.Value.ExportedAt);
        Assert.Equal(envelope.State.ToJsonString(), parsed.Value.State.ToJsonString());
    }
}
