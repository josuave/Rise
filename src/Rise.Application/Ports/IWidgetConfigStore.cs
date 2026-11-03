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

    /// <summary>Every widget's saved config JSON, keyed by widget id. Empty when nothing is stored or storage is corrupt.</summary>
    Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
}
