using System.Text.Json.Nodes;
using Rise.Application.Services;
using Rise.Slices.Settings.Abstractions;

namespace Rise.Slices.Header;

/// <summary>
/// The clock widget's configuration: a self-managing object over
/// its JSON. It owns its schema (24-hour format, seconds) and its
/// evolution (currently v1: no migrations yet, so Migrate is an
/// identity); the widget-config service runs the migration on load
/// and saves the migrated JSON back. Missing keys read as defaults.
/// </summary>
public sealed class ClockConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => ClockWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; missing keys read as defaults.</summary>
    public JsonObject Data { get; private set; } = new();

    public bool Use24Hour
    {
        get => WidgetConfigJson.GetBool(Data, nameof(Use24Hour));
        set => Data[nameof(Use24Hour)] = value;
    }

    public bool ShowSeconds
    {
        get => WidgetConfigJson.GetBool(Data, nameof(ShowSeconds));
        set => Data[nameof(ShowSeconds)] = value;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        Data = await service.LoadMigratedAsync(WidgetId, CurrentVersion, Migrate, cancellationToken);

    public async Task SaveAsync(CancellationToken cancellationToken = default) =>
        await service.SetJsonAsync(WidgetId, Data.ToJsonString(), cancellationToken);
}

/// <summary>
/// The greeting widget's configuration: a self-managing object over
/// its JSON. It owns its schema (custom text, blank meaning the
/// automatic time-based greeting; name, used in the automatic
/// greeting when set) and its evolution (currently v1:
/// no migrations yet, so Migrate is an identity); the widget-config
/// service runs the migration on load and saves the migrated JSON
/// back. A missing key reads as the default.
/// </summary>
public sealed class GreetingConfig(WidgetConfigService service) : SelfMigratingWidgetConfig
{
    public override string WidgetId => GreetingWidgetDefinition.WidgetId;

    public override int CurrentVersion => 1;

    public override JsonObject Migrate(JsonObject data, int fromVersion) => data;

    /// <summary>The loaded config JSON. Empty until LoadAsync runs; a missing key reads as the default.</summary>
    public JsonObject Data { get; private set; } = new();

    public string? CustomText
    {
        get => WidgetConfigJson.GetString(Data, nameof(CustomText));
        set
        {
            if (value is null)
            {
                Data.Remove(nameof(CustomText));
            }
            else
            {
                Data[nameof(CustomText)] = value;
            }
        }
    }

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
