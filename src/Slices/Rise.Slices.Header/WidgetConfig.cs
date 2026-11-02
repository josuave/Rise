using System.Text.Json;
using Rise.Application.Services;

namespace Rise.Slices.Header;

/// <summary>
/// The header widgets' config shapes and their JSON round-trip
/// through the generic widget-config service. The service stores
/// strings keyed by widget id; only this slice knows what those
/// strings mean. Corrupt or absent JSON falls back to defaults.
/// </summary>
internal static class WidgetConfig
{
    internal sealed record ClockConfig(bool Use24Hour, bool ShowSeconds)
    {
        internal static ClockConfig Default => new(false, false);
    }

    internal sealed record GreetingConfig(string? CustomText)
    {
        internal static GreetingConfig Default => new((string?)null);
    }

    internal static async Task<ClockConfig> LoadClockAsync(WidgetConfigService service, CancellationToken cancellationToken = default)
    {
        string? json = await service.GetJsonAsync(ClockWidgetDefinition.WidgetId, cancellationToken);
        return Parse(json, ClockConfig.Default);
    }

    internal static Task SaveClockAsync(WidgetConfigService service, ClockConfig config, CancellationToken cancellationToken = default) =>
        service.SetJsonAsync(ClockWidgetDefinition.WidgetId, JsonSerializer.Serialize(config), cancellationToken);

    internal static async Task<GreetingConfig> LoadGreetingAsync(WidgetConfigService service, CancellationToken cancellationToken = default)
    {
        string? json = await service.GetJsonAsync(GreetingWidgetDefinition.WidgetId, cancellationToken);
        return Parse(json, GreetingConfig.Default);
    }

    internal static Task SaveGreetingAsync(WidgetConfigService service, GreetingConfig config, CancellationToken cancellationToken = default) =>
        service.SetJsonAsync(GreetingWidgetDefinition.WidgetId, JsonSerializer.Serialize(config), cancellationToken);

    private static T Parse<T>(string? json, T fallback) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
