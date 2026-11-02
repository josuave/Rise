namespace Rise.Application.Ports;

/// <summary>
/// Outbound port: per-widget settings, keyed by widget id, as JSON.
/// The core never knows a widget's config shape — the slice that owns
/// the widget owns its shape and (de)serializes it; this port just
/// stores the string.
/// </summary>
public interface IWidgetConfigStore
{
    Task<string?> GetAsync(string widgetId, CancellationToken cancellationToken = default);

    Task SetAsync(string widgetId, string json, CancellationToken cancellationToken = default);
}
