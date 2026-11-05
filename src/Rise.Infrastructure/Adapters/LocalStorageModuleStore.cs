using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists explicit module enable/disable choices in
/// localStorage under "rise.modules.v1" — one object keyed by module
/// id — through the same riseStorage helper the other stores use.
/// Corrupt or unavailable storage reads as no saved choices (every
/// module enabled); a failed save leaves the page working for this
/// session.
/// </summary>
public sealed class LocalStorageModuleStore(IJSRuntime js) : IModuleStore
{
    private const string StorageKey = "rise.modules.v1";

    public async Task<Dictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return new Dictionary<string, bool>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, bool>>(stored) ?? new Dictionary<string, bool>();
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            return new Dictionary<string, bool>();
        }
    }

    public async Task SaveAllAsync(Dictionary<string, bool> choices, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(choices));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }

    public async Task SetAsync(string moduleId, bool enabled, CancellationToken cancellationToken = default)
    {
        Dictionary<string, bool> choices;
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            try
            {
                choices = string.IsNullOrWhiteSpace(stored)
                    ? new Dictionary<string, bool>()
                    : JsonSerializer.Deserialize<Dictionary<string, bool>>(stored) ?? new Dictionary<string, bool>();
            }
            catch (JsonException)
            {
                choices = new Dictionary<string, bool>();
            }

            choices[moduleId] = enabled;
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(choices));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}
