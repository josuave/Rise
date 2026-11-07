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

        BackupEnvelope envelope = ResultAssertions.RequireOk(parsed);
        Assert.Equal(1, envelope.FormatVersion);
    }

    [Fact]
    public void Parse_RejectsInvalidJson()
    {
        Result<BackupEnvelope> parsed = BackupFormat.Parse("not json");

        Assert.NotEmpty(ResultAssertions.RequireError(parsed));
    }

    [Fact]
    public void Parse_RejectsNewerVersion()
    {
        JsonObject root = JsonNode.Parse(BackupFormat.Serialize(V1Envelope()))!.AsObject();
        root["FormatVersion"] = 2;

        Result<BackupEnvelope> parsed = BackupFormat.Parse(root.ToJsonString());

        Assert.Contains("newer", ResultAssertions.RequireError(parsed), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RejectsMissingState()
    {
        JsonObject root = JsonNode.Parse(BackupFormat.Serialize(V1Envelope()))!.AsObject();
        root.Remove("State");

        Result<BackupEnvelope> parsed = BackupFormat.Parse(root.ToJsonString());

        Assert.NotEmpty(ResultAssertions.RequireError(parsed));
    }

    [Fact]
    public void Migrate_ToOwnVersion_IsIdentity()
    {
        BackupEnvelope envelope = V1Envelope();

        Result<BackupEnvelope> migrated = BackupFormat.Migrate(envelope, 1);

        BackupEnvelope migratedEnvelope = ResultAssertions.RequireOk(migrated);
        Assert.Equal(1, migratedEnvelope.FormatVersion);
        Assert.Equal(envelope.State.ToJsonString(), migratedEnvelope.State.ToJsonString());
    }

    [Fact]
    public void Migrate_Backwards_Fails()
    {
        Result<BackupEnvelope> migrated = BackupFormat.Migrate(V1Envelope(), 0);

        Assert.NotEmpty(ResultAssertions.RequireError(migrated));
    }

    [Fact]
    public void Migrate_BeyondCurrent_Fails()
    {
        Result<BackupEnvelope> migrated = BackupFormat.Migrate(V1Envelope(), BackupFormat.CurrentVersion + 1);

        Assert.NotEmpty(ResultAssertions.RequireError(migrated));
    }

    [Fact]
    public void Serialize_RoundTripsThroughParse()
    {
        BackupEnvelope envelope = V1Envelope();

        Result<BackupEnvelope> parsed = BackupFormat.Parse(BackupFormat.Serialize(envelope));

        BackupEnvelope roundTripped = ResultAssertions.RequireOk(parsed);
        Assert.Equal(envelope.FormatVersion, roundTripped.FormatVersion);
        Assert.Equal(envelope.ExportedAt, roundTripped.ExportedAt);
        Assert.Equal(envelope.State.ToJsonString(), roundTripped.State.ToJsonString());
    }
}
