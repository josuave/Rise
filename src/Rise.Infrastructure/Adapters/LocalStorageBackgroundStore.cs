using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the chosen background in localStorage
/// under "rise.background.v1" — one JSON object with the preset id
/// and the custom image address — through the same riseStorage helper
/// the other stores use. Corrupt or unavailable storage reads as no
/// saved choice (plain theme background); a failed save leaves the
/// page working for this session.
/// </summary>
public sealed class LocalStorageBackgroundStore(IJSRuntime js) : IBackgroundStore
{
    private const string StorageKey = "rise.background.v1";

    public async Task<BackgroundChoice?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            return JsonSerializer.Deserialize<BackgroundChoice>(stored);
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SaveAsync(BackgroundChoice choice, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(choice));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}
