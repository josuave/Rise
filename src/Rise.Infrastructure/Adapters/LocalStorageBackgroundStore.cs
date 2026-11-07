using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the chosen background in localStorage
/// under "rise.background.v1" — one JSON object with the preset id
/// and the custom image address — through the same riseStorage helper
/// the other stores use. The stored shape stays a plain string
/// object (the union lives in memory only): a private DTO carries
/// the JSON fields, and <see cref="BackgroundChoice.FromStored"/> /
/// <see cref="BackgroundChoice.ToStored"/> map at this boundary.
/// Corrupt or unavailable storage reads as no saved choice (plain
/// theme background); a failed save leaves the page working for
/// this session.
/// </summary>
public sealed class LocalStorageBackgroundStore(IJSRuntime js) : IBackgroundStore
{
    private const string StorageKey = "rise.background.v1";

    /// <summary>The stored JSON shape: the preset id, plus the image address when the choice is custom, plus the uploaded image's descriptor when the choice is an upload.</summary>
    private sealed record StoredBackground(string PresetId, string? CustomUrl, UploadedImage? Upload = null);

    public async Task<BackgroundChoice?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            StoredBackground? dto = JsonSerializer.Deserialize<StoredBackground>(stored);
            if (dto is null)
            {
                return null;
            }

            return BackgroundChoice.FromStored(dto.PresetId, dto.CustomUrl, dto.Upload);
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
            (string presetId, string? customUrl, UploadedImage? upload) = choice.ToStored();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(new StoredBackground(presetId, customUrl, upload)));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }
}
