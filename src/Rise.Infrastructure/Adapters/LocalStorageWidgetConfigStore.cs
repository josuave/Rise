using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists per-widget config JSON in localStorage
/// under "rise.widgets.v1" — one object keyed by widget id — through
/// the same riseStorage helper the other stores use. Corrupt or
/// unavailable storage reads as no saved config; corrupt stored data
/// is replaced by the next save, and a failed save leaves the page
/// working for this session.
/// </summary>
public sealed class LocalStorageWidgetConfigStore(IJSRuntime js) : IWidgetConfigStore
{
    private const string StorageKey = "rise.widgets.v1";

    public async Task<string?> GetAsync(string widgetId, CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            Dictionary<string, string>? parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(stored);
            if (parsed is null || !parsed.TryGetValue(widgetId, out string? json))
            {
                return null;
            }

            return json;
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return new Dictionary<string, string>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(stored) ?? new Dictionary<string, string>();
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            return new Dictionary<string, string>();
        }
    }

    public async Task SetAsync(string widgetId, string json, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> configs;
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            try
            {
                configs = string.IsNullOrWhiteSpace(stored)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(stored) ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                configs = new Dictionary<string, string>();
            }

            configs[widgetId] = json;
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(configs));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}
