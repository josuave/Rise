using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: writes the Omarchy pack palettes to localStorage
/// under rise.omarchy-themes.v1, through the same riseStorage helper
/// the other stores use. The shell's before-paint script reads this
/// key to apply a pack choice before first paint; the app itself
/// never reads it back.
/// </summary>
public sealed class LocalStorageOmarchyThemeCache(IJSRuntime js) : IOmarchyThemeCache
{
    public const string StorageKey = "rise.omarchy-themes.v1";

    public async Task WriteCacheAsync(IReadOnlyList<OmarchyTheme> themes, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(themes));
        }
        catch (JSException)
        {
            // The cache only avoids a first-paint flash; a failed write must not break the page.
        }
    }
}
