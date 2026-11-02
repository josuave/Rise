using Rise.Application.Ports;

namespace Rise.Application.Services;

/// <summary>
/// Imperative shell for widget settings: load and save one widget's
/// config JSON, and notify listeners when any widget's config changes
/// so widgets on the page re-render with their new settings. The
/// service knows widget ids only as strings — config shapes belong
/// to the slices that own the widgets.
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

    public async Task SetJsonAsync(string widgetId, string json, CancellationToken cancellationToken = default)
    {
        await store.SetAsync(widgetId, json, cancellationToken);
        ConfigChanged?.Invoke(this, new WidgetConfigChangedEventArgs(widgetId));
    }
}
