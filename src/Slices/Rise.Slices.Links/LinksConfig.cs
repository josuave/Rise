using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Domain;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Links;

/// <summary>
/// The Links widget's configuration: Compact or Detailed view, and
/// the sort order. Self-managing over its JSON (v1, identity
/// Migrate). Defaults are Compact and Custom order. An unknown
/// stored view reads as Compact; an unknown stored sort reads as
/// Custom. Saved under the Links widget id, so it rides backup
/// wholesale with the rest of the widget configs.
/// </summary>
public sealed class LinksConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => LinksWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    /// <summary>"Compact" or "Detailed". Default Compact.</summary>
    public string ViewMode
    {
        get
        {
            string? stored = WidgetConfigJson.GetString(Data, nameof(ViewMode));
            return string.Equals(stored, "Detailed", StringComparison.OrdinalIgnoreCase) ? "Detailed" : "Compact";
        }

        set => Data[nameof(ViewMode)] = value;
    }

    /// <summary>True when the detailed card view is active.</summary>
    public bool IsDetailed => string.Equals(ViewMode, "Detailed", StringComparison.Ordinal);

    /// <summary>The sort order for the widget. Default Custom (stored order).</summary>
    public LinkSort Sort
    {
        get
        {
            string? stored = WidgetConfigJson.GetString(Data, nameof(Sort));
            return Enum.TryParse(stored, out LinkSort sort) ? sort : LinkSort.Custom;
        }

        set => Data[nameof(Sort)] = value.ToString();
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}
