using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists explicit hotkey binding overrides in
/// localStorage under "rise.hotkeys.v1" — one object keyed by action
/// id — through the same riseStorage helper the other stores use.
/// Corrupt or unavailable storage reads as no overrides (every action
/// on its default); a failed save leaves the page working for this
/// session.
/// </summary>
public sealed class LocalStorageHotkeyStore(IJSRuntime js) : IHotkeyStore
{
    private const string StorageKey = "rise.hotkeys.v1";

    public async Task<Dictionary<string, HotkeyBinding>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return new Dictionary<string, HotkeyBinding>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, HotkeyBinding>>(stored) ?? new Dictionary<string, HotkeyBinding>();
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            return new Dictionary<string, HotkeyBinding>();
        }
    }

    public async Task SaveAllAsync(Dictionary<string, HotkeyBinding> bindings, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(bindings));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }

    public async Task SetAsync(string actionId, HotkeyBinding binding, CancellationToken cancellationToken = default)
    {
        Dictionary<string, HotkeyBinding> bindings = await GetAllAsync(cancellationToken);
        bindings[actionId] = binding;
        await SaveAllAsync(bindings, cancellationToken);
    }
}
