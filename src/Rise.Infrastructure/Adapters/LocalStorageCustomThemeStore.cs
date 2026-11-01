using System.Text.Json;
using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: persists the user's custom themes in the
/// browser's localStorage under "rise.themes.v1", through the same
/// riseStorage helper the other stores use. Corrupt or unavailable
/// storage returns null and the caller falls back to an empty set.
/// </summary>
public sealed class LocalStorageCustomThemeStore(IJSRuntime js) : ICustomThemeStore
{
    private const string StorageKey = "rise.themes.v1";

    private sealed record StoredPalette(
        string Background,
        string Text,
        string Muted,
        string Faint,
        string Surface,
        string Border,
        string BorderSoft,
        string TileBackground,
        string Error);

    private sealed record StoredTheme(string Id, string Name, StoredPalette Light, StoredPalette Dark);

    public async Task<IReadOnlyList<CustomTheme>?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string? stored = await js.InvokeAsync<string?>("riseStorage.get", cancellationToken, StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            List<StoredTheme>? parsed = JsonSerializer.Deserialize<List<StoredTheme>>(stored);
            if (parsed is null)
            {
                return null;
            }

            return parsed
                .Where(theme => !string.IsNullOrWhiteSpace(theme.Id) && theme.Light is not null && theme.Dark is not null)
                .GroupBy(theme => theme.Id)
                .Select(group => group.First())
                .Select(theme => new CustomTheme(
                    theme.Id,
                    theme.Name,
                    ToPalette(theme.Light),
                    ToPalette(theme.Dark)))
                .ToList();
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Corrupt or unavailable storage: the caller falls back to an empty set.
            return null;
        }
    }

    public async Task SaveAsync(IReadOnlyList<CustomTheme> themes, CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = themes
                .Select(theme => new StoredTheme(
                    theme.Id,
                    theme.Name,
                    FromPalette(theme.Light),
                    FromPalette(theme.Dark)))
                .ToList();
            await js.InvokeVoidAsync("riseStorage.set", cancellationToken, StorageKey, JsonSerializer.Serialize(stored));
        }
        catch (JSException)
        {
            // Storage unavailable (e.g. private mode): the page still works for this session.
        }
    }

    private static ThemePalette ToPalette(StoredPalette stored) => new(
        stored.Background,
        stored.Text,
        stored.Muted,
        stored.Faint,
        stored.Surface,
        stored.Border,
        stored.BorderSoft,
        stored.TileBackground,
        stored.Error);

    private static StoredPalette FromPalette(ThemePalette palette) => new(
        palette.Background,
        palette.Text,
        palette.Muted,
        palette.Faint,
        palette.Surface,
        palette.Border,
        palette.BorderSoft,
        palette.TileBackground,
        palette.Error);
}
