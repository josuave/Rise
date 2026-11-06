using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Search;

/// <summary>
/// The search widget's configuration: which engine the search box
/// and the command palette use. Self-managing over its JSON (v1,
/// identity Migrate). An unknown stored engine id reads as Google.
/// </summary>
public sealed class SearchConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => SearchWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    /// <summary>The configured engine id; unknown or missing reads as Google.</summary>
    public string EngineId
    {
        get => SearchEngines.FromId(WidgetConfigJson.GetString(Data, nameof(EngineId))).Id;
        set => Data[nameof(EngineId)] = value;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}
