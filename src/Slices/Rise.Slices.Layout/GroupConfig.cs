using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Layout;

/// <summary>
/// The group widget's configuration: a self-managing object over its
/// JSON. It owns its schema (just the display name, blank meaning the
/// default "Group") and its evolution (currently v1: no migrations yet,
/// so Migrate is an identity); the widget-config service runs the
/// migration on load and saves the migrated JSON back. Missing keys
/// read as defaults.
/// </summary>
public sealed class GroupConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => GroupWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    public string? Name
    {
        get => WidgetConfigJson.GetString(Data, nameof(Name));
        set
        {
            if (value is null)
            {
                Data.Remove(nameof(Name));
            }
            else
            {
                Data[nameof(Name)] = value;
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}
