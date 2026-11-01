using Microsoft.JSInterop;
using Rise.Application.Ports;
using Rise.Domain;

namespace Rise.Infrastructure.Adapters;

/// <summary>
/// Outbound adapter: theme preference via the <c>window.riseTheme</c>
/// script block in the shell's index.html (localStorage + the
/// document's data-theme attribute + matchMedia). The core knows
/// nothing about localStorage or JS; the adapter implements these.
/// </summary>
public sealed class ThemeStore(IJSRuntime js) : IThemeStore
{
    private const string StorageKey = "rise.theme.v1";

    public async Task<string?> GetStoredThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("riseTheme.getStored", cancellationToken, StorageKey);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task SetStoredThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseTheme.setStored", cancellationToken, StorageKey, theme);
        }
        catch (JSException)
        {
            // Theme preference is a nicety; a failed save must not break the page.
        }
    }

    public async Task ApplyThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync("riseTheme.apply", cancellationToken, theme);
        }
        catch (JSException)
        {
            // Same: a failed apply must not break the page.
        }
    }

    public async Task ApplyCustomThemeAsync(ThemePalette palette, CancellationToken cancellationToken = default)
    {
        try
        {
            // Keys are the calm CSS variable names from app.css.
            var variables = new Dictionary<string, string>
            {
                ["--calm-bg"] = palette.Background,
                ["--calm-text"] = palette.Text,
                ["--calm-muted"] = palette.Muted,
                ["--calm-faint"] = palette.Faint,
                ["--calm-surface"] = palette.Surface,
                ["--calm-border"] = palette.Border,
                ["--calm-border-soft"] = palette.BorderSoft,
                ["--calm-tile-bg"] = palette.TileBackground,
                ["--calm-error"] = palette.Error,
            };
            await js.InvokeVoidAsync("riseTheme.applyCustom", cancellationToken, variables);
        }
        catch (JSException)
        {
            // Same: a failed apply must not break the page.
        }
    }

    public async Task<bool> PrefersDarkAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("riseTheme.prefersDark", cancellationToken);
        }
        catch (JSException)
        {
            return false;
        }
    }
}
