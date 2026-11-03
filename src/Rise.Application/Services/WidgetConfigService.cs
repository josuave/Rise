using System.Text.Json;
using System.Text.Json.Nodes;
using Rise.Application.Ports;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for widget settings: load and save one widget's
/// config JSON, and notify listeners when any widget's config changes
/// so widgets on the page re-render with their new settings. The
/// service knows widget ids only as strings — config shapes belong
/// to the slices that own the widgets. For self-managing configs it
/// also runs their migration on load: slices pass their config's
/// Migrate method as a delegate, so the service never references a
/// config type and the core keeps no slice dependency.
/// </summary>
/// <summary>Event data for a widget config change: which widget's config changed.</summary>
public sealed class WidgetConfigChangedEventArgs(string widgetId) : EventArgs
{
    public string WidgetId { get; } = widgetId;
}

public sealed class WidgetConfigService(IWidgetConfigStore store)
{
    /// <summary>Fires when a widget's config is saved.</summary>
    public event EventHandler<WidgetConfigChangedEventArgs>? ConfigChanged;

    public Task<string?> GetJsonAsync(string widgetId, CancellationToken cancellationToken = default) =>
        store.GetAsync(widgetId, cancellationToken);

    /// <summary>
    /// Loads one widget's config as a JSON object, migrating it when
    /// its stored version is older than <paramref name="currentVersion"/>
    /// and saving the migrated JSON back. Absent, blank, or corrupt
    /// JSON loads as an empty object (typed getters default). A
    /// missing version field means v1; a version newer than the
    /// app knows passes through untouched, never downgraded or
    /// wiped. The save-back goes through the store directly, not
    /// the change event: the loading widget re-renders from its own
    /// load, and no other widget reads this widget's config.
    /// </summary>
    public async Task<JsonObject> LoadMigratedAsync(
        string widgetId,
        int currentVersion,
        Func<JsonObject, int, JsonObject> migrate,
        CancellationToken cancellationToken = default)
    {
        string? stored = await store.GetAsync(widgetId, cancellationToken);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return new JsonObject();
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(stored);
        }
        catch (JsonException)
        {
            return new JsonObject();
        }

        if (node is not JsonObject data)
        {
            return new JsonObject();
        }

        int storedVersion = StoredVersion(data);
        if (storedVersion >= currentVersion)
        {
            return data;
        }

        JsonObject migrated;
        try
        {
            migrated = migrate(data, storedVersion);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // A failing migration must not wipe the stored config: return it unmigrated.
            return data;
        }

        migrated["Version"] = currentVersion;
        await store.SetAsync(widgetId, migrated.ToJsonString(), cancellationToken);
        return migrated;
    }

    private static int StoredVersion(JsonObject data) =>
        data["Version"] is JsonValue value && value.TryGetValue<int>(out int version) && version >= 1 ? version : 1;

    public async Task SetJsonAsync(string widgetId, string json, CancellationToken cancellationToken = default)
    {
        await store.SetAsync(widgetId, json, cancellationToken);
        ConfigChanged?.Invoke(this, new WidgetConfigChangedEventArgs(widgetId));
    }
}
